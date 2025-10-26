using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLama.Fun.Mcp;

/// <summary>
/// Integrates Ollama (llama3.2) with MCP tools for AI-powered database operations
/// </summary>
public class OllamaMcpIntegration
{
    private readonly HttpClient _httpClient;
    private readonly McpServer _mcpServer;
    private readonly string _modelName;
    private readonly List<Dictionary<string, object>> _conversationHistory;

    public OllamaMcpIntegration(string ollamaUrl = "http://localhost:11434", string modelName = "llama3.2")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ollamaUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
        _modelName = modelName;
        _conversationHistory = new List<Dictionary<string, object>>();

        // Initialize MCP server with all tools
        _mcpServer = new McpServer();
        McpUserToolsAdapter.RegisterUserTools(_mcpServer);
        McpUserToolsAdapter.RegisterUserResources(_mcpServer);
    }

    /// <summary>
    /// Process a user query using Ollama with MCP tool access
    /// </summary>
    public async Task<string> ProcessQueryAsync(string userQuery)
    {
        // Add user message to history
        _conversationHistory.Add(new Dictionary<string, object>
        {
            { "role", "user" },
            { "content", userQuery }
        });

        // Get available tools from MCP
        var toolsRequest = new McpRequest
        {
            Id = Guid.NewGuid().ToString(),
            Method = "tools/list"
        };
        var toolsResponse = await _mcpServer.HandleRequestAsync(toolsRequest);
        var mcpTools = ExtractMcpTools(toolsResponse);

        // Convert MCP tools to Ollama format
        var ollamaTools = ConvertMcpToolsToOllamaFormat(mcpTools);

        // Call Ollama with tool support
        var response = await CallOllamaWithToolsAsync(_conversationHistory, ollamaTools);

        // Check if Ollama wants to call tools
        if (response.ContainsKey("message") && response["message"] is JsonElement messageElement)
        {
            var message = messageElement;

            // Add assistant message to history
            _conversationHistory.Add(new Dictionary<string, object>
            {
                { "role", "assistant" },
                { "content", message.TryGetProperty("content", out var content) ? content.GetString() ?? "" : "" },
                { "tool_calls", message.TryGetProperty("tool_calls", out var toolCalls) ? toolCalls : new JsonElement() }
            });

            // Execute tool calls if present
            if (message.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array)
            {
                foreach (var toolCall in calls.EnumerateArray())
                {
                    var toolResult = await ExecuteToolCallAsync(toolCall);

                    // Add tool result to history
                    _conversationHistory.Add(new Dictionary<string, object>
                    {
                        { "role", "tool" },
                        { "content", toolResult }
                    });
                }

                // Call Ollama again with tool results
                response = await CallOllamaWithToolsAsync(_conversationHistory, ollamaTools);

                if (response.ContainsKey("message") && response["message"] is JsonElement finalMessage)
                {
                    if (finalMessage.TryGetProperty("content", out var finalContent))
                    {
                        var finalResponse = finalContent.GetString() ?? "";
                        _conversationHistory.Add(new Dictionary<string, object>
                        {
                            { "role", "assistant" },
                            { "content", finalResponse }
                        });
                        return finalResponse;
                    }
                }
            }
            else if (message.TryGetProperty("content", out var directContent))
            {
                // No tool calls, return direct response
                return directContent.GetString() ?? "";
            }
        }

        return "I couldn't process your request.";
    }

    /// <summary>
    /// Call Ollama API with tool support
    /// </summary>
    private async Task<Dictionary<string, object>> CallOllamaWithToolsAsync(
        List<Dictionary<string, object>> messages,
        List<object> tools)
    {
        var requestBody = new
        {
            model = _modelName,
            messages = messages,
            tools = tools,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/chat", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson)
               ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Execute an MCP tool call from Ollama
    /// </summary>
    private async Task<string> ExecuteToolCallAsync(JsonElement toolCall)
    {
        try
        {
            var functionName = toolCall.GetProperty("function").GetProperty("name").GetString();
            var argumentsJson = toolCall.GetProperty("function").GetProperty("arguments").GetString();

            if (string.IsNullOrEmpty(functionName))
                return "Error: Tool name is missing";

            // Parse arguments
            Dictionary<string, object>? arguments = null;
            if (!string.IsNullOrEmpty(argumentsJson))
            {
                arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson);
            }

            // Create MCP tool call request
            var mcpRequest = new McpRequest
            {
                Id = Guid.NewGuid().ToString(),
                Method = "tools/call",
                Params = new Dictionary<string, object>
                {
                    { "name", functionName },
                    { "arguments", arguments ?? new Dictionary<string, object>() }
                }
            };

            // Execute via MCP
            var mcpResponse = await _mcpServer.HandleRequestAsync(mcpRequest);

            if (mcpResponse.Error != null)
            {
                return $"Error: {mcpResponse.Error.Message}";
            }

            // Extract result
            if (mcpResponse.Result is JsonElement resultElement)
            {
                if (resultElement.TryGetProperty("content", out var contentArray) &&
                    contentArray.ValueKind == JsonValueKind.Array)
                {
                    var firstContent = contentArray.EnumerateArray().FirstOrDefault();
                    if (firstContent.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? "";
                    }
                }
            }

            return JsonSerializer.Serialize(mcpResponse.Result);
        }
        catch (Exception ex)
        {
            return $"Error executing tool: {ex.Message}";
        }
    }

    /// <summary>
    /// Extract MCP tools from the tools/list response
    /// </summary>
    private List<McpTool> ExtractMcpTools(McpResponse response)
    {
        var tools = new List<McpTool>();

        if (response.Result is JsonElement resultElement)
        {
            if (resultElement.TryGetProperty("tools", out var toolsArray) &&
                toolsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var tool in toolsArray.EnumerateArray())
                {
                    var name = tool.GetProperty("name").GetString();
                    var description = tool.GetProperty("description").GetString();
                    var inputSchema = tool.GetProperty("inputSchema");

                    if (name != null && description != null)
                    {
                        tools.Add(new McpTool
                        {
                            Name = name,
                            Description = description,
                            InputSchema = JsonSerializer.Deserialize<object>(inputSchema.GetRawText()) ?? new { }
                        });
                    }
                }
            }
        }

        return tools;
    }

    /// <summary>
    /// Convert MCP tool format to Ollama tool format
    /// </summary>
    private List<object> ConvertMcpToolsToOllamaFormat(List<McpTool> mcpTools)
    {
        var ollamaTools = new List<object>();

        foreach (var mcpTool in mcpTools)
        {
            ollamaTools.Add(new
            {
                type = "function",
                function = new
                {
                    name = mcpTool.Name,
                    description = mcpTool.Description,
                    parameters = mcpTool.InputSchema
                }
            });
        }

        return ollamaTools;
    }

    /// <summary>
    /// Clear conversation history
    /// </summary>
    public void ClearHistory()
    {
        _conversationHistory.Clear();
    }

    /// <summary>
    /// Get conversation history
    /// </summary>
    public List<Dictionary<string, object>> GetHistory()
    {
        return new List<Dictionary<string, object>>(_conversationHistory);
    }
}
