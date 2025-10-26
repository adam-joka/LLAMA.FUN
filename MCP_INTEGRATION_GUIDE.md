# MCP Integration Guide for LLama.Fun

This guide explains how to use the Model Context Protocol (MCP) integration in the LLama.Fun application.

## What is MCP?

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to LLMs. It enables:
- **Tools**: Functions that LLMs can call to perform actions
- **Resources**: Data that can be read by LLMs
- **Prompts**: Reusable prompt templates

## Architecture Overview

The MCP integration consists of several components:

```
Mcp/
├── McpModels.cs              # Data models for MCP messages
├── McpServer.cs              # Core MCP server implementation
├── McpUserToolsAdapter.cs    # Adapts UserCrudHandler to MCP tools
├── McpStdioServer.cs         # Stdio transport for MCP
└── Examples/
    └── McpClientExample.cs   # Example client code
```

## Quick Start

### 1. Running the Example Client

To see MCP in action with examples, you can call the example directly from your Program.cs or use it programmatically:

```csharp
using LLama.Fun.Mcp.Examples;

// Run all examples
await McpClientExample.RunExamplesAsync();
```

This will demonstrate:
- Initializing the MCP connection
- Listing available tools
- Calling tools (add_user, list_users, etc.)
- Reading resources

### 2. Running as an MCP Server (Stdio Mode)

To run as a standalone MCP server that communicates via stdin/stdout, you can call it from your Program.cs:

```csharp
using LLama.Fun.Mcp;

// Start MCP server
var mcpServer = new McpStdioServer();
await mcpServer.StartAsync();
```

The server will:
- Listen for JSON-RPC messages on stdin
- Send responses on stdout
- Log errors to stderr

### 3. Using MCP in Your Code

You can also integrate MCP directly in your application:

```csharp
using LLama.Fun.Mcp;

var server = new McpServer();
McpUserToolsAdapter.RegisterUserTools(server);
McpUserToolsAdapter.RegisterUserResources(server);

// Handle an MCP request
var request = new McpRequest
{
    Id = "1",
    Method = "tools/list"
};

var response = await server.HandleRequestAsync(request);
```

## Available Tools

The following tools are exposed via MCP:

### 1. add_user
Add a new user to the database.

**Parameters:**
- `name` (string, required): User's full name
- `email` (string, required): User's email address

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/call",
  "params": {
    "name": "add_user",
    "arguments": {
      "name": "Jane Doe",
      "email": "jane@example.com"
    }
  }
}
```

### 2. get_user
Get a user by ID or name.

**Parameters:**
- `id` (integer, optional): User ID
- `name` (string, optional): User name

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "tools/call",
  "params": {
    "name": "get_user",
    "arguments": {
      "id": 1
    }
  }
}
```

### 3. list_users
List all users in the database.

**Parameters:** None

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "tools/call",
  "params": {
    "name": "list_users"
  }
}
```

### 4. update_user
Update an existing user's information.

**Parameters:**
- `id` (integer, required): User ID to update
- `name` (string, optional): New name
- `email` (string, optional): New email

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": "4",
  "method": "tools/call",
  "params": {
    "name": "update_user",
    "arguments": {
      "id": 1,
      "email": "newemail@example.com"
    }
  }
}
```

### 5. delete_user
Delete a user from the database.

**Parameters:**
- `id` (integer, required): User ID to delete

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": "5",
  "method": "tools/call",
  "params": {
    "name": "delete_user",
    "arguments": {
      "id": 1
    }
  }
}
```

## Available Resources

### 1. users://all
Complete list of all users in the database.

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": "6",
  "method": "resources/read",
  "params": {
    "uri": "users://all"
  }
}
```

### 2. users://schema
Database schema for the users table.

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": "7",
  "method": "resources/read",
  "params": {
    "uri": "users://schema"
  }
}
```

## MCP Methods

### initialize
Initialize the MCP connection.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "clientInfo": {
      "name": "YourClient",
      "version": "1.0.0"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "protocolVersion": "2024-11-05",
    "serverInfo": {
      "name": "LLama.Fun MCP Server",
      "version": "1.0.0"
    },
    "capabilities": {
      "tools": {},
      "resources": {}
    }
  }
}
```

### tools/list
List all available tools.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "tools/list"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "result": {
    "tools": [
      {
        "name": "add_user",
        "description": "Add a new user to the database with name and email",
        "inputSchema": { ... }
      },
      ...
    ]
  }
}
```

### resources/list
List all available resources.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "resources/list"
}
```

## Integration with Claude Desktop

To use this MCP server with Claude Desktop, add this configuration to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "llama-fun": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\REPOS\\LLAMA.FUN\\LLama.Fun", "Mcp/McpStdioServer.cs"]
    }
  }
}
```

## Using MCP in Your Own Code

### Creating a Custom MCP Server

```csharp
using LLama.Fun.Mcp;

var server = new McpServer();

// Register a custom tool
server.RegisterTool(
    name: "my_tool",
    description: "Does something useful",
    inputSchema: new
    {
        type = "object",
        properties = new
        {
            param1 = new { type = "string" }
        }
    },
    handler: async (args) =>
    {
        // Your custom logic here
        return new McpToolResult
        {
            Content = new List<McpContent>
            {
                new McpContent
                {
                    Type = "text",
                    Text = "Result of my_tool"
                }
            },
            IsError = false
        };
    }
);

// Handle requests
var request = new McpRequest
{
    Id = "1",
    Method = "tools/call",
    Params = new Dictionary<string, object>
    {
        { "name", "my_tool" },
        { "arguments", new { param1 = "value" } }
    }
};

var response = await server.HandleRequestAsync(request);
```

### Registering Custom Resources

```csharp
server.RegisterResource(
    uri: "myapp://data",
    name: "My Data",
    description: "Custom application data",
    mimeType: "application/json",
    handler: async () =>
    {
        // Return your data as JSON string
        return JsonSerializer.Serialize(new { data = "value" });
    }
);
```

## Error Handling

MCP uses JSON-RPC 2.0 error codes:

- `-32700`: Parse error
- `-32600`: Invalid request
- `-32601`: Method not found
- `-32602`: Invalid params
- `-32603`: Internal error

**Error Response Example:**
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "error": {
    "code": -32602,
    "message": "Unknown tool: invalid_tool",
    "data": null
  }
}
```

## Testing

Run the example client to test all functionality:

```bash
dotnet run --project LLama.Fun Mcp/Examples/McpClientExample.cs
```

This will execute a series of operations demonstrating:
- Tool listing
- Tool execution
- Resource reading
- Error handling

## Next Steps

1. **Extend Tools**: Add more tools in `McpUserToolsAdapter.cs`
2. **Add Resources**: Expose more data via resources
3. **Custom Transport**: Implement HTTP or WebSocket transport
4. **Security**: Add authentication/authorization
5. **Integration**: Connect to Claude Desktop or other MCP clients

## References

- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [MCP Documentation](https://modelcontextprotocol.io/)
- [JSON-RPC 2.0](https://www.jsonrpc.org/specification)
