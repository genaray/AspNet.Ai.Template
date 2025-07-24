namespace AuthenticationService.Feature.UserServiceProxy;

/// <summary>
/// The <see cref="UserServiceSettings"/> class
/// contains settings for the UserService.
/// </summary>
public class UserServiceSettings
{
    public required string BaseUrl { get; set; }
    public required string LangChain { get; set; }
    
    public string LangChainUrl => $"{BaseUrl}/{LangChain}";
}
