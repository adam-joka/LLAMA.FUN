# How to Run MCP Examples

The MCP integration is now ready to use! Here are several ways to test it:

## Option 1: Add to Your Existing Program.cs

Add this to your `Program.cs` to test MCP:

```csharp
using LLama.Fun.Mcp.Examples;

Console.WriteLine("Running MCP Examples...\n");
await McpClientExample.RunExamplesAsync();
```

Then run:
```bash
dotnet run
```

## Option 2: Use MCP Server Mode

To run as an MCP server that can be integrated with Claude Desktop or other MCP clients:

Add this to your `Program.cs`:

```csharp
using LLama.Fun.Mcp;

Console.WriteLine("Starting MCP Server...");
var server = new McpStdioServer();
await server.StartAsync();
```

Then configure Claude Desktop to use it by adding to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "llama-fun": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\REPOS\\LLAMA.FUN\\LLama.Fun\\LLama.Fun.csproj"],
      "env": {}
    }
  }
}
```

## Option 3: Use MCP in Your Code

Integrate MCP directly into your application:

```csharp
using LLama.Fun.Mcp;
using System.Text.Json;

// Create and configure MCP server
var mcpServer = new McpServer();
McpUserToolsAdapter.RegisterUserTools(mcpServer);
McpUserToolsAdapter.RegisterUserResources(mcpServer);

// Example: List all tools
var listToolsRequest = new McpRequest
{
    Id = "1",
    Method = "tools/list"
};

var response = await mcpServer.HandleRequestAsync(listToolsRequest);
Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));

// Example: Call add_user tool
var addUserRequest = new McpRequest
{
    Id = "2",
    Method = "tools/call",
    Params = new Dictionary<string, object>
    {
        { "name", "add_user" },
        { "arguments", new Dictionary<string, object>
            {
                { "name", "Test User" },
                { "email", "test@example.com" }
            }
        }
    }
};

response = await mcpServer.HandleRequestAsync(addUserRequest);
Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
```

## What the Examples Will Show

When you run the examples, you'll see:

1. **Initialize** - MCP connection setup
2. **List Tools** - All 5 CRUD tools (add_user, get_user, list_users, update_user, delete_user)
3. **Add User** - Create a test user
4. **List Users** - Display all users
5. **Get User** - Retrieve specific user
6. **Update User** - Modify user data
7. **List Resources** - Show available resources (users://all, users://schema)
8. **Read Resource** - Access the users resource

All responses will be in JSON-RPC 2.0 format with proper error handling.
