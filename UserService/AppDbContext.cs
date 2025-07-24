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
        if (!context.Users.Any())
        {
            var response = await client.GetUserCredentialsIdByEmail("admin@example.com");

            if (!response.Result.Success)
            {
                logger.LogError("Could not fetch User from AuthenticationService.");
                throw new InvalidOperationException("Could not fetch User from AuthenticationService.");
            }

            // Add user
            var adminUser = new User
            {
                Id = response.UserId,
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
            logger.LogInformation("Fetched {User}.", adminUser);
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
