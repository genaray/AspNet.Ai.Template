using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ModelContextProtocol.AspNetCore;
using UserService.Feature.AppUser;
using UserService.Feature.Authentication;
using UserService.Feature.Mcp;
using HealthCheckService = UserService.Feature.HealthCheck.HealthCheckService;
using LangChain.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UserService.Feature.LangChain;

[assembly: InternalsVisibleTo("AuthenticationService.Tests")]
namespace UserService;

using HealthCheckService = HealthCheckService;

public class Program
{
    private const string Name = "UserService";
    
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add asp.net & prometheus services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Extract settings
        builder.Services.Configure<McpSettings>(builder.Configuration.GetSection("McpService"));
        
        // Http-Connection to AuthService
        builder.Services.AddHttpClient<IAuthClient, AuthClient>(client =>
        {
            // Address of service e.g. url or kubernetes service name
            client.BaseAddress = new Uri(builder.Configuration["AuthService:BaseUrl"] ?? throw new ArgumentException("AuthService:BaseUrl is not set."));
        });
        
        // Http-Connection to McpService (itself)
        builder.Services.AddHttpClient<LangChainService>( (sp, client) =>
        {
            var mcpSettings = sp.GetRequiredService<IOptions<McpSettings>>().Value;
            client.BaseAddress = new Uri(mcpSettings.BaseUrl ?? throw new ArgumentException("McpService:BaseUrl is not set."));
        });
        
        // Custom services
        builder.Services.AddScoped<Feature.AppUser.UserService>();
        
        // AI
        builder.Services.AddMcpServer().WithHttpTransport().WithTools<McpUserTools>().WithTools<EchoTool>().WithToolsFromAssembly();
        builder.Services.AddOpenAi();
        builder.Services.AddAnthropic();
        
        // Health check
        builder.Services.AddHealthChecks().AddCheck<HealthCheckService>(nameof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService));

        // Connect to PostgreSQL
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        //builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("teste"));
        
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
                setup.SwaggerDoc("v1", new OpenApiInfo { Title = "User-API", Version = "v1" });
                
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
        }

        // Create the database if it doesn't exist
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var authClient = scope.ServiceProvider.GetRequiredService<IAuthClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
            
            dbContext.Database.EnsureDeleted();  // Be careful with EnsureDeleted(), it deletes the database
            dbContext.Database.EnsureCreated();  // Ensures the database is created

            try
            {
                AppDbContext.SeedAsync(logger, dbContext, authClient).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                logger.LogError(e.StackTrace);
                throw;
            }
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
        app.MapMcp("/api/mcp/invoke");

        // 6. SPA‑Fallback for static files
        app.MapFallbackToFile("index.html");
        
        app.Run();
    }
}