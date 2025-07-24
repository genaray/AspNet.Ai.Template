using System.Runtime.CompilerServices;
using System.Text;
using AuthenticationService.Feature.Email;
using AuthenticationService.Feature.Frontend;
using AuthenticationService.Feature.UserCredentials;
using AuthenticationService.Feature.UserServiceProxy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using HealthCheckService = AuthenticationService.Feature.HealthCheck.HealthCheckService;
using Polly;
using Polly.Extensions.Http;

[assembly: InternalsVisibleTo("AuthenticationService.Tests")]
namespace AuthenticationService;

using HealthCheckService = Feature.HealthCheck.HealthCheckService;

// https://grafana.com/grafana/dashboards/20568-opentelemetry-dotnet-webapi/

public class Program
{
    private const string Name = "AuthenticationService";
    
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add asp.net & prometheus services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Map appsettings to setting classes
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
        builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));
        builder.Services.Configure<UserServiceSettings>(builder.Configuration.GetSection("UserService"));
        
        // Custom services
        builder.Services.AddScoped<EmailTemplateRenderer>();
        builder.Services.AddScoped<EmailSender>();
        builder.Services.AddScoped<EmailService>();
        builder.Services.AddScoped<Feature.Authentication.AuthenticationService>();
        builder.Services.AddScoped<UserCredentialsService>();
        
        // Http-Connection to UserService for LangChainProxy
        builder.Services.AddHttpClient<UserServiceProxyService>( (sp, client) =>
        {
            var userServiceSettings = sp.GetRequiredService<IOptions<UserServiceSettings>>().Value;
            client.BaseAddress = new Uri(userServiceSettings.BaseUrl ?? throw new ArgumentException("UserService:BaseUrl is not set."));
        })
        .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        }));
        
        // Health check
        builder.Services.AddHealthChecks().AddCheck<HealthCheckService>(nameof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService));

        // Connect to PostgreSQL
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        //builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("teste"));
        
        // Add identity to efcore
        builder.Services.AddIdentity<UserCredentials, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        
        // Adding Authentication & JWT-Bearer
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:ValidAudience"],  // Access config files and insert value
                    ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
                };
            });

        // Conditional logic for development environment (optional)
        if (builder.Environment.IsDevelopment())
        {
            // Disable cors by allowing from all originsAdd commentMore actions
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin() 
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            
            builder.Services.AddSwaggerGen(setup =>
            {
                setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Authentication-API", Version = "v1" });
                
                // Include 'SecurityScheme' to use JWT Authentication
                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    BearerFormat = "JWT",
                    Name = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
                setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });
            });
        }

        // Configure the HTTP request pipeline
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            Console.WriteLine("Started in Development Mode.");
            Console.WriteLine("Swagger-UI is available.");
        }

        // Create the database if it doesn't exist
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserCredentials>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            dbContext.Database.EnsureDeleted();  // Be careful with EnsureDeleted(), it deletes the database
            dbContext.Database.EnsureCreated();  // Ensures the database is created
            AppDbContext.SeedAsync(userManager, roleManager).Wait();
        }
        
        // 1. HTTPS
        app.UseHttpsRedirection();

        // 2. Static files
        app.UseDefaultFiles();   
        app.UseStaticFiles();     // CSS/JS/Pictures

        // 3. Routing & CORS
        app.UseRouting();
        app.UseCors("AllowAll");

        // 4. AuthN & AuthZ
        app.UseAuthentication();
        app.UseAuthorization();

        // 5. API-Endpoints
        app.MapControllers();
        app.MapHealthChecks("/health");

        // 6. SPA‑Fallback for static files
        app.MapFallbackToFile("index.html");
        
        app.Run();
    }
}