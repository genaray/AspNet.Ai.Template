using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using UserService.Feature.AppUser;

namespace UserService.Feature.Mcp;

/// <summary>
/// The <see cref="McpUserTools"/> class
/// Defines a set of tools for user management that can be invoked by an MCP client.
/// <code>
/// Automatic generated endpoint awaits json grpc request with the following format:
///  {
///     "jsonrpc": "2.0",
///     "id": 1,
///     "method": "tools/call",
///     "params": {
///     "name": "The name of the Tool to use, given by mcp_tool",
///         "arguments": {
///             "id": "The Id of the entity to perform the action on",
///             "firstName": "The first name",
///             "lastName": "The last name",
///             ... Other parameters
///         }
///     }
/// }
/// </code>
/// </summary>
[McpServerToolType]
public class McpUserTools()
{
    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    /// <param name="userService">The user service for database operations.</param>
    /// <param name="id">The ID of the user to update.</param>
    /// <param name="firstName">The new first name of the user.</param>
    /// <param name="lastName">The new last name of the user.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    [McpServerTool(Name = "UpdateUser"), Description("Updates an existing user")]
    public static async Task<bool> UpdateUser(
        AppUser.UserService userService,
        [Description("The user's id, required")] string id,
        [Description("The user's first name, required, can be empty")] string firstName,
        [Description("The user's last name, required, can be empty")] string lastName)
    {
        var userDto = new CreateOrUpdateUserDto
        {
            FirstName = firstName,
            LastName = lastName
        };
        return await userService.UpdateUserAsync(id, userDto);
    }

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="userService">The user service for database operations.</param>
    /// <param name="id">The ID of the user to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    [McpServerTool(Name = "DeleteUser"), Description("Deletes a user")]
    public static async Task<bool> DeleteUser(
        AppUser.UserService userService,
        [Description("The user's id, required")] string id)
    {
        return await userService.DeleteUserAsync(id);
    }
}

/// <summary>
/// A simple tool for echoing a message.
/// </summary>
[McpServerToolType]
public class EchoTool {

    /// <summary>
    /// Echoes the given message back to the client.
    /// </summary>
    /// <param name="message">The message to echo.</param>
    /// <returns>The echoed message with a "hello" prefix.</returns>
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}