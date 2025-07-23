using LangChain.Chains;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using Microsoft.Extensions.Options;
using UserService.Feature.Mcp;

namespace UserService.Feature.LangChain;

/// <summary>
/// The <see cref="LangChainService"/> class
/// is a Service for processing natural language queries using LangChain.
/// This service orchestrates the use of different tools (MCP, SQL) to answer user queries.
/// </summary>
public class LangChainService
{
    private readonly OpenAiLatestFastChatModel _model;
    private readonly McpTool _mcpTool;
    private readonly McpExecutionTool _mcpExecutionTool;
    private readonly SqlSchemaTool _sqlSchemaTool;
    private readonly SqlExecutionTool _sqlExecutionTool;

    /// <summary>
    /// Initializes a new instance of the <see cref="LangChainService"/> class.
    /// </summary>
    /// <param name="config">The application configuration.</param>
    /// <param name="mcpSettings">The mcp settings</param>
    /// <param name="appDbContext">The database context.</param>
    /// <param name="httpClient">The HTTP client.</param>
    public LangChainService(IConfiguration config, IOptions<McpSettings> mcpSettings, AppDbContext appDbContext, HttpClient httpClient)
    {
        var apiKey = config["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI-ApiKey not configured");
        _model = new OpenAiLatestFastChatModel(apiKey).UseConsoleForDebug();
        _model.Settings = ChatSettings.Default;
        _model.Settings.StopSequences = ["Final Answer:", "final answer"];
        
        _mcpTool = new McpTool(mcpSettings.Value, httpClient);
        _mcpExecutionTool = new McpExecutionTool(mcpSettings.Value, httpClient);
        _sqlSchemaTool = new SqlSchemaTool(appDbContext);
        _sqlExecutionTool = new SqlExecutionTool(appDbContext);
    }

    /// <summary>
    /// Processes a user's query using a ReAct agent and a set of tools.
    /// </summary>
    /// <param name="userInput">The user's query.</param>
    /// <returns>The final answer from the LangChain agent.</returns>
    public async Task<string> ProcessAsync(string userInput)
    {
        var promptTemplate =
            @"""
            Do not generate code. 
            Keep the answer as short as possible. Always quote the context in your answer.
            Context: {toolResult}
            Question: {request}
            Final Answer:
            """
            ;
        
        // Sets the input, chooses and executes different tools, parses the toolResult into the prompt template and finally returns the final answer.
        var chain = Chain.Set(userInput, outputKey: "request") |
                    Chain.ReActAgentExecutor(_model, inputKey: "request", outputKey: "toolResult").UseTool(_mcpTool).UseTool(_sqlSchemaTool).UseTool(_sqlExecutionTool).UseTool(_mcpExecutionTool) | 
                    Chain.Template(promptTemplate, outputKey: "prompt") | 
                    Chain.LLM(_model, inputKey: "prompt", outputKey: "finalAnswer");

        return await chain.RunAsync("finalAnswer") ?? string.Empty;
    }

}