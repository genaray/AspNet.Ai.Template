using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using UserService.Feature.AppUser;

namespace UserService.Feature.LangChain;

/// <summary>
/// The <see cref="LangChainController"/> class
/// is a Controller for handling LangChain requests.
/// This controller provides an endpoint for processing natural language queries using the LangChain service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LangChainController(LangChainService langChainService) : ControllerBase
{

    /// <summary>
    /// Processes a natural language query using the LangChain service.
    /// </summary>
    /// <param name="input">The natural language query to process.</param>
    /// <returns>A string containing the result of the LangChain processing.</returns>
    [HttpGet]
    public async Task<ActionResult<string>> GetUsers(string input)
    {
        var output = await langChainService.ProcessAsync(input);
        return Ok(output);
    }
}