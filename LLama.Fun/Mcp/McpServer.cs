using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLama.Fun.Mcp;

/// <summary>
/// MCP Server implementation for exposing tools and resources
/// </summary>
public class McpServer
{
    private readonly Dictionary<string, Func<Dictionary<string, object>?, Task<McpToolResult>>> _tools = new();
    private readonly List<McpTool> _toolDefinitions = new();
    private readonly Dictionary<string, Func<Task<string>>> _resources = new();
    private readonly List<McpResource> _resourceDefinitions = new();

    /// <summary>
    /// Register a tool that can be called via MCP
    /// </summary>
    public void RegisterTool(string name, string description, object inputSchema,
        Func<Dictionary<string, object>?, Task<McpToolResult>> handler)
    {
        _tools[name] = handler;
        _toolDefinitions.Add(new McpTool
        {
            Name = name,
            Description = description,
            InputSchema = inputSchema
        });
    }

    /// <summary>
    /// Register a resource that can be accessed via MCP
    /// </summary>
    public void RegisterResource(string uri, string name, string description,
        string mimeType, Func<Task<string>> handler)
    {
        _resources[uri] = handler;
        _resourceDefinitions.Add(new McpResource
        {
            Uri = uri,
            Name = name,
            Description = description,
            MimeType = mimeType
        });
    }

    /// <summary>
    /// Handle incoming MCP request
    /// </summary>
    public async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        var response = new McpResponse { Id = request.Id };

        try
        {
            response.Result = request.Method switch
            {
                "initialize" => HandleInitialize(request.Params),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolCallAsync(request.Params),
                "resources/list" => HandleResourcesList(),
                "resources/read" => await HandleResourceReadAsync(request.Params),
                _ => throw new McpException(-32601, $"Method not found: {request.Method}")
            };
        }
        catch (McpException ex)
        {
            response.Error = new McpError
            {
                Code = ex.Code,
                Message = ex.Message,
                Data = ex.Data
            };
        }
        catch (Exception ex)
        {
            response.Error = new McpError
            {
                Code = -32603,
                Message = "Internal error",
                Data = ex.Message
            };
        }

        return response;
    }

    /// <summary>
    /// Handle MCP initialize request
    /// </summary>
    private object HandleInitialize(Dictionary<string, object>? @params)
    {
        return new
        {
            protocolVersion = "2024-11-05",
            serverInfo = new
            {
                name = "LLama.Fun MCP Server",
                version = "1.0.0"
            },
            capabilities = new
            {
                tools = new { },
                resources = new { }
            }
        };
    }

    /// <summary>
    /// List all available tools
    /// </summary>
    private object HandleToolsList()
    {
        return new { tools = _toolDefinitions };
    }

    /// <summary>
    /// Execute a tool call
    /// </summary>
    private async Task<object> HandleToolCallAsync(Dictionary<string, object>? @params)
    {
        if (@params == null || !@params.ContainsKey("name"))
        {
            throw new McpException(-32602, "Missing 'name' parameter");
        }

        var toolName = @params["name"].ToString()!;

        if (!_tools.ContainsKey(toolName))
        {
            throw new McpException(-32602, $"Unknown tool: {toolName}");
        }

        Dictionary<string, object>? arguments = null;
        if (@params.ContainsKey("arguments"))
        {
            // Parse arguments from JSON
            var argsJson = JsonSerializer.Serialize(@params["arguments"]);
            arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argsJson);
        }

        var result = await _tools[toolName](arguments);
        return result;
    }

    /// <summary>
    /// List all available resources
    /// </summary>
    private object HandleResourcesList()
    {
        return new { resources = _resourceDefinitions };
    }

    /// <summary>
    /// Read a resource
    /// </summary>
    private async Task<object> HandleResourceReadAsync(Dictionary<string, object>? @params)
    {
        if (@params == null || !@params.ContainsKey("uri"))
        {
            throw new McpException(-32602, "Missing 'uri' parameter");
        }

        var uri = @params["uri"].ToString()!;

        if (!_resources.ContainsKey(uri))
        {
            throw new McpException(-32602, $"Unknown resource: {uri}");
        }

        var content = await _resources[uri]();

        return new
        {
            contents = new[]
            {
                new
                {
                    uri = uri,
                    mimeType = "application/json",
                    text = content
                }
            }
        };
    }

    /// <summary>
    /// Process JSON-RPC message from stdin
    /// </summary>
    public static async Task<McpResponse> ProcessMessageAsync(string jsonMessage, McpServer server)
    {
        var request = JsonSerializer.Deserialize<McpRequest>(jsonMessage);

        if (request == null)
        {
            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -32700,
                    Message = "Parse error"
                }
            };
        }

        return await server.HandleRequestAsync(request);
    }
}

/// <summary>
/// Custom MCP exception
/// </summary>
public class McpException : Exception
{
    public int Code { get; }
    public new object? Data { get; }

    public McpException(int code, string message, object? data = null) : base(message)
    {
        Code = code;
        Data = data;
    }
}
