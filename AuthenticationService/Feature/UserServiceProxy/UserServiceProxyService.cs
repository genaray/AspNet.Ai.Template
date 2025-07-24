using Microsoft.Extensions.Options;
using Shared;

namespace AuthenticationService.Feature.UserServiceProxy;

public class UserServiceProxyService(IOptions<UserServiceSettings> settings, ILogger<UserServiceProxyService> logger, HttpClient httpClient)
{

    public async Task<Response<string>> InvokeLangChain(string request)
    {
        try
        {
            // Auf den "input"-Query param umstellen
            var url = $"{settings.Value.LangChain}?input={Uri.EscapeDataString(request)}";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var answer = await response.Content.ReadAsStringAsync();
            return new Response<string>
            {
                Result = new Result{ Success = true },
                Data = answer
            };
        } catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error calling UserService LangChain endpoint.");
            return new Response<string>
            {
                Result = new Result{ Success = false },
                Data = "Error"
            };
        }
    }
    
}