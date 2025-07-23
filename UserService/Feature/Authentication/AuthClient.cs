using System.Net;
using Shared;

namespace UserService.Feature.Authentication;

public class RegisterCredentialsResponse
{
    public string Message { get; set; } = null!;
    public string UserId  { get; set; } = null!;
}

public class RegisterResponse
{
    public Result Result { get; set; }
    public string UserId  { get; set; } = null!;
}

public class GetUserCredentialsByIdResponse
{
    public string Id { get; set; } = null!;
}

/// <summary>
/// Interface for the authentication service client.
/// </summary>
public interface IAuthClient
{
    /// <summary>
    /// Registers a user's credentials with the authentication service.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>A <see cref="RegisterResponse"/> indicating the result of the operation.</returns>
    Task<RegisterResponse> RegisterUserCredentialsAsync(string username, string email, string password);

    /// <summary>
    /// Gets a user's credentials ID by their email.
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <returns>A <see cref="RegisterResponse"/> containing the user's ID if found.</returns>
    Task<RegisterResponse> GetUserCredentialsIdByEmail(string email);
}

/// <summary>
/// Client to communicate with the authentication service.
/// </summary>
public class AuthClient : IAuthClient
{
    private readonly HttpClient _http;

    public AuthClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Registers a user's credentials with the authentication service.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>A <see cref="RegisterResponse"/> indicating the result of the operation.</returns>
    public async Task<RegisterResponse> RegisterUserCredentialsAsync(string username, string email, string password)
    {
        // DTO that is expected in the AuthService
        var dto = new
        {
            Username = username,
            Email    = email,
            Password = password,
        };

        // Call auth service
        var response = await _http.PostAsJsonAsync("/api/Authenticate/register", dto);

        // Already exists
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return new RegisterResponse
            {
                Result = new Result{ Success = false, Error = new UserAlreadyExistsException(errorPayload!.Message)},
            };
        }

        response.EnsureSuccessStatusCode();

        // Deserialize response
        var deserializedResponse = await response.Content.ReadFromJsonAsync<RegisterCredentialsResponse>();
        return new RegisterResponse
        {
            Result = new Result{ Success = true }, 
            UserId = deserializedResponse!.UserId
        };
    }
    
    /// <summary>
    /// Gets a user's credentials ID by their email.
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <returns>A <see cref="RegisterResponse"/> containing the user's ID if found.</returns>
    public async Task<RegisterResponse> GetUserCredentialsIdByEmail(string email)
    {
        // Call auth service
        var response = await _http.GetAsync($"/api/Authenticate/{email}");

        // Already exists
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new RegisterResponse
            {
                Result = new Result{ Success = false, Error = new UserNotFoundException()},
            };
        }

        response.EnsureSuccessStatusCode();

        // Deserialize response
        var deserializedResponse = await response.Content.ReadFromJsonAsync<GetUserCredentialsByIdResponse>();
        return new RegisterResponse
        {
            Result = new Result{ Success = true }, 
            UserId = deserializedResponse!.Id
        };
    }
}