namespace UserService.Feature.Mcp;

/// <summary>
/// The <see cref="McpSettings"/> class
/// contains all set settings to reference the Mcp-Server. 
/// </summary>
public class McpSettings
{
    public required string BaseUrl { get; set; }
    public required string Tools { get; set; }
    public required string Execution { get; set; }
    
    public string ToolsUrl => $"{BaseUrl}/{Tools}";
    
    public string ExecutionUrl => $"{BaseUrl}/{Execution}";
}