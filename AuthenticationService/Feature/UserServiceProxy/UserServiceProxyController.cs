using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthenticationService.Feature.UserServiceProxy;

[Route("api/[controller]")]
[ApiController]
public class UserServiceProxyController(UserServiceProxyService userService, ILogger<UserServiceProxyService> logger) : ControllerBase
{
    [HttpPost("langChain")]
    public async Task<IActionResult> InvokeLangChain([FromBody] string request)
    {
        var response = await userService.InvokeLangChain(request);
        var test=  response.Result.Error != null ? response.Result.Error.ToIActionResult() : Ok(response.Data);
        logger.LogInformation(test.ToString());
        return test;
    }
}

public class LangChainRequest
{
    public string Prompt { get; set; } = null!;
}
