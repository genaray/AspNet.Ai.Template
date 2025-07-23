using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using UserService.Feature.LangChain;

namespace UserService.Feature.Mcp;

/// <summary>
/// The <see cref="McpController"/> class
/// is a Controller for handling Model-Context-Protocol (MCP) requests.
/// This controller discovers and exposes available tools for MCP clients.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class McpController : ControllerBase
{
    /// <summary>
    /// Retrieves a list of all available MCP tools.
    /// This endpoint scans the assembly for methods and classes annotated with MCP attributes
    /// and returns a JSON representation of the available tools, their descriptions, and parameters.
    /// </summary>
    /// <returns>An IResult containing the JSON representation of the MCP tools.</returns>
    [HttpGet]
    public IResult GetMcpTools()
    {
        var tools = typeof(McpUserTools).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null)
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
            .Select(m => new
            {
                Name        = m.GetCustomAttribute<McpServerToolAttribute>()!.Name,
                Description = m.GetCustomAttribute<DescriptionAttribute>()?.Description,
                Parameters  = m.GetParameters().Select(p => new
                {
                    Name        = p.Name,
                    Type        = p.ParameterType.Name,
                    Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description
                })
            });
        return Results.Json(tools);
    }
}

