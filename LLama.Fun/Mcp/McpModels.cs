using System.Text.Json.Serialization;

namespace LLama.Fun.Mcp;

/// <summary>
/// Base MCP message structure
/// </summary>
public class McpMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// MCP Request from client
/// </summary>
public class McpRequest : McpMessage
{
    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }
}

/// <summary>
/// MCP Response to client
/// </summary>
public class McpResponse : McpMessage
{
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

/// <summary>
/// MCP Error details
/// </summary>
public class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Tool definition for MCP
/// </summary>
public class McpTool
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("inputSchema")]
    public required object InputSchema { get; set; }
}

/// <summary>
/// Resource definition for MCP
/// </summary>
public class McpResource
{
    [JsonPropertyName("uri")]
    public required string Uri { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}

/// <summary>
/// Tool call parameters
/// </summary>
public class McpToolCall
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }
}

/// <summary>
/// Tool execution result
/// </summary>
public class McpToolResult
{
    [JsonPropertyName("content")]
    public required List<McpContent> Content { get; set; }

    [JsonPropertyName("isError")]
    public bool IsError { get; set; }
}

/// <summary>
/// Content block in MCP messages
/// </summary>
public class McpContent
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
