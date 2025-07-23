using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using UserService.Feature.AppUser;
using UserService.Feature.Authentication;

namespace UserService;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // To snake case
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToLower());
            foreach (var prop in entity.GetProperties())
                prop.SetColumnName(prop.GetColumnName().ToLower());
        }
    }

    /// <summary>
    ///     Seed users and roles in the Identity database.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="context">The <see cref="AppDbContext"/>.</param>
    /// <param name="client">The <see cref="IAuthClient"/>.</param>
    /// <returns></returns>
    public static async Task SeedAsync(ILogger<AppDbContext> logger, AppDbContext context, IAuthClient client)
    {
        const int maxRetries = 3;
        const int delayMs = 1000; 
        var attempt = 0;
        
        /*// Add user
        var user = new User
        {
            Id = "123",
            FirstName = "Admin",
            LastName = "",
        };

        try
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogWarning("Admin user already exists. Skipping insert.");
        }*/

        // return;
        
        if (!context.Users.Any())
        {
            // Retry loop
            RegisterResponse? response = null;
            while (attempt < maxRetries)
            {
                response = await client.GetUserCredentialsIdByEmail("admin@example.com");

                // Success, break
                if (response.Result.Success)
                {
                    break;
                }

                attempt++;
                if (attempt >= maxRetries)
                {
                    logger.LogError("Could not fetch User from AuthenticationService after {Attempts} attempts.", attempt);
                    throw new InvalidOperationException($"Could not fetch User from AuthenticationService.");
                }

                // Pause
                await Task.Delay(delayMs);
            }

            // Add user
            var adminUser = new User
            {
                Id = response!.UserId,
                FirstName = "Admin",
                LastName = "",
            };

            try
            {
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                logger.LogWarning("Admin user already exists. Skipping insert.");
            }
            logger.LogInformation("Fetched {User} after {Attempts} attempts.", adminUser, attempt);;
        }
    }
    
    /// <summary>
    /// Checks if a duplicate key exception is thrown.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <returns>True or false.</returns>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key") ?? false;
    }
}
