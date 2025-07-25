using System.Data;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LangChain.Chains.StackableChains.Agents.Crew.Tools;
using LangChain.Chains.StackableChains.Agents.Tools;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using tryAGI.OpenAI;
using UserService.Extensions;
using UserService.Feature.Mcp;

namespace UserService.Feature.LangChain;

/// <summary>
/// An agent tool for listing all possible MCP (Model-Context-Protocol) actions.
/// </summary>
public class McpTool(McpSettings mcpSettings, HttpClient client) : AgentTool("mcp_tool", "Lists all possible MCP actions on domain entities and business logic.")
{
    /// <summary>
    /// Fetches the list of available MCP tools from the MCP endpoint.
    /// </summary>
    /// <param name="input">The input for the tool (not used in this case).</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A string containing the JSON representation of the available MCP tools.</returns>
    public override async Task<string> ToolTask(string input, CancellationToken token = new())
    {
        var response = await client.GetAsync($"/{mcpSettings.Tools}", token);
        return await response.Content.ReadAsStringAsync(token);
    }
}

/// <summary>
/// An agent tool for executing MCP (Model-Context-Protocol) API calls.
/// </summary>
public class McpExecutionTool(McpSettings mcpSettings, HttpClient client) : AgentTool(
    "mcp_execute_tool", 
    """
    Executes actual MCP API calls. Use when an action should be performed.
    Insert the following example JSON into the tool input (check which parameters are required by using mcp_tool):
    {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "tools/call",
        "params": {
          "name": "The name of the Tool to use, given by mcp_tool",
          "arguments": {
            "id": "The Id of the entity to perform the action on",
            "firstName": "The first name",
            "lastName": "The last name",
            ... Other parameters
          }
        }
    }
    Make sure that the Json is valid and that there no Comments or # in the Json.
    The jsonrpc, id and method fields should not be changed. 
    """
)
{
    /// <summary>
    /// Executes an MCP API call based on the provided JSON input.
    /// </summary>
    /// <param name="input">A JSON string representing the MCP request.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A string containing the response from the MCP endpoint.</returns>
    public override async Task<string> ToolTask(string input, CancellationToken token = new())
    {
        // Remove potential comments from input, otherwhise error
        var cleanedInput = input.RemoveJsonComments();

        // Create post message
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"/{mcpSettings.Execution}"
        );
        message.Content = new StringContent(
            cleanedInput,
            Encoding.UTF8,
            "application/json"
        );
        message.Headers.Accept.Clear(); // optional
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
 
        // Send request and get response 
        var response = await client.SendAsync(message, token);
        return await response.Content.ReadAsStringAsync(token);
    }
}

/// <summary>
/// An agent tool for listing all table names and their columns in the current database schema.
/// </summary>
public class SqlSchemaTool(AppDbContext dbContext) : AgentTool("sql_schema_tool", "Lists all table names and their columns in the current database schema.")
{
    /// <summary>
    /// Retrieves the database schema and returns it as a JSON string.
    /// </summary>
    /// <param name="input">The input for the tool (not used in this case).</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A JSON string representing the database schema.</returns>
    public override async Task<string> ToolTask(string input, CancellationToken token = default)
    {
        // Open connection
        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(token);

        // SQL to list all table names and column in public schema
        const string sql = @"
            SELECT table_name, column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
            ORDER BY table_name, ordinal_position;
        ";

        // Execute command
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;

        await using var reader = await cmd.ExecuteReaderAsync(token);

        // Collect results in dictionary
        var schema = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(token))
        {
            var table = reader.GetString(0);
            var column = reader.GetString(1);

            if (!schema.TryGetValue(table, out var cols))
            {
                cols = new List<string>();
                schema[table] = cols;
            }
            cols.Add(column);
        }

        // To json
        var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        return json;
    }
}

/// <summary>
/// An agent tool for executing complex SELECT queries, aggregations, or joins.
/// </summary>
public class SqlExecutionTool(AppDbContext dbContext) : AgentTool(
    "sql_execution_tool", 
    """
    Use for complex SELECT queries, aggregations or joins.
    Provide the SQL query as input. 
    Only the SQL query, no additional data is required.
    """
)
{
    /// <summary>
    /// Executes a SQL SELECT query and returns the result as a JSON string.
    /// </summary>
    /// <param name="input">The SQL SELECT query to execute.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A JSON string representing the query result, or an error message if the query fails.</returns>
    public override async Task<string> ToolTask(string input, CancellationToken token = new())
    {
        // Prevent SQL escalation from the ai 
        var sql = input.Trim();
        if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return await Task.FromResult("Error: Only SELECT statements are allowed.");
        }

        try
        {
            // Open connection 
            var conn = dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(token);

            // Execute query
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            await using var reader = await cmd.ExecuteReaderAsync(token);
            var rows = new List<Dictionary<string, object>>();

            // Read query and collect results in dictionary
            while (await reader.ReadAsync(token))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                rows.Add(row);
            }

            // Return to llm
            var json = JsonSerializer.Serialize(rows);
            return await Task.FromResult(json);
        }
        catch (Exception ex)
        {
            return await Task.FromResult($"SQL execution error: {ex.Message}");
        }
    }
}