using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared;
using UserService.Feature.Authentication;

namespace UserService.Feature.AppUser;

/// <summary>
/// The <see cref="UserService"/> class
/// acts as a service layer to abstract <see cref="User"/> related operations from its controller. 
/// </summary>
/// <param name="context">The <see cref="AppDbContext"/>.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
public class UserService(
    ILogger<UserService> logger, 
    AppDbContext context, 
    IAuthClient authClient
)
{
    /// <summary>
    /// Returns all users filtered by a <see cref="searchTerm"/> and pagination;
    /// </summary>
    /// <param name="searchTerm">The term.</param>
    /// <param name="page">The page.</param>
    /// <param name="pageSize">The page-size.</param>
    /// <returns>A <see cref="Task"/> with an <see cref="IEnumerable{T}"/> of <see cref="User"/>s and its total count.</returns>
    /// <exception cref="ArgumentException">Thrown if the passed <see cref="page"/> and <see cref="pageSize"/> are equal or less than zero.</exception>
    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(string searchTerm, int page, int pageSize)
    {
        logger.LogInformation("Fetching users...");
        if (page <= 0 || pageSize <= 0)
        {
            throw new ArgumentException("Page and PageSize must be greater than zero.");
        }

        // Search
        var query = context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => EF.Functions.Like(u.FirstName, $"%{searchTerm}%"));
        }

        var totalCount = await query.CountAsync();
        var pagedUsers = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (pagedUsers, totalCount);
    }

    /// <summary>
    /// Returns an <see cref="User"/> by its id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A <see cref="Task"/> with the <see cref="User"/>.</returns>
    public async Task<User?> GetUserByIdAsync(string id)
    {
        logger.LogInformation($"Fetching user with ID {id}...");
        return await context.Users.FindAsync(id);
    }

    /// <summary>
    /// Creates an <see cref="User"/> async. 
    /// </summary>
    /// <param name="username">His username.</param>
    /// <param name="email">His email.</param>
    /// <param name="password">His password.</param>
    /// <param name="firstName">His firstname.</param>
    /// <param name="lastName">His lastname.</param>
    /// <returns>An <see cref="Task{TResult}"/> with User-Data indicating whether the User was created or not.</returns>
    internal async Task<Response<User>> CreateUserAsync(string username, string email, string password, string firstName, string lastName)
    {
        // Register credentials on authservice
        var registerResponse = await authClient.RegisterUserCredentialsAsync(username, email, password);
        if (!registerResponse.Result.Success)
        {
            return new Response<User>
            {
                Result = registerResponse.Result,
                Data = null
            };   
        }
        
        // Create user
        var user = new User
        {
            Id = registerResponse.UserId,
            FirstName = firstName,
            LastName = lastName,
        };
        
        // Save user
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        // Response
        return new Response<User>
        {
            Result = new Result { Success = true },
            Data = user
        };  
    }

    /// <summary>
    /// Updates an <see cref="User"/> by its id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="userDto">The <see cref="CreateOrUpdateUserDto"/>.</param>
    /// <returns>A <see cref="Task"/> with a bool indicating if the operation was successfully.</returns>
    public async Task<bool> UpdateUserAsync(string id, CreateOrUpdateUserDto userDto)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            logger.LogWarning($"User with ID {id} not found.");
            return false;
        }
        
        user.MergeWith(userDto);
        
        try
        {
            await context.SaveChangesAsync();
            logger.LogInformation($"User with ID {id} updated.");
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (await GetUserByIdAsync(user.Id) == null) throw;
            logger.LogWarning($"User with ID {id} not found.");
            return false;
        }
    }

    /// <summary>
    /// Deletes an <see cref="User"/> by its id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A <see cref="Task"/> with a bool indicating if the operation was successfully.</returns>
    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            logger.LogWarning($"User with ID {id} not found.");
            return false;
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync();
        logger.LogInformation($"User with ID {id} deleted.");
        return true;
    }
}
