using System.Text.Json;

namespace LLama.Fun.Mcp.Examples;

/// <summary>
/// Example MCP client that demonstrates how to interact with the MCP server
/// </summary>
public class McpClientExample
{
    private readonly McpServer _server;

    public McpClientExample()
    {
        _server = new McpServer();
        McpUserToolsAdapter.RegisterUserTools(_server);
        McpUserToolsAdapter.RegisterUserResources(_server);
    }

    /// <summary>
    /// Run example interactions with the MCP server
    /// </summary>
    public static async Task RunExamplesAsync()
    {
        var client = new McpClientExample();

        Console.WriteLine("=== MCP Client Examples ===\n");

        // Initialize database
        using var context = new ApplicationDbContext();
        await context.Database.EnsureCreatedAsync();

        // Example 1: Initialize
        await client.ExampleInitialize();

        // Example 2: List tools
        await client.ExampleListTools();

        // Example 3: Add a user
        await client.ExampleAddUser();

        // Example 4: List users
        await client.ExampleListUsers();

        // Example 5: Get user by ID
        await client.ExampleGetUser();

        // Example 6: Update user
        await client.ExampleUpdateUser();

        // Example 7: List resources
        await client.ExampleListResources();

        // Example 8: Read resource
        await client.ExampleReadResource();

        Console.WriteLine("\n=== Examples Complete ===");
    }

    private async Task ExampleInitialize()
    {
        Console.WriteLine("1. Initialize MCP Connection");

        var request = new McpRequest
        {
            Id = "1",
            Method = "initialize",
            Params = new Dictionary<string, object>
            {
                { "protocolVersion", "2024-11-05" },
                { "clientInfo", new { name = "ExampleClient", version = "1.0.0" } }
            }
        };

        var response = await _server.HandleRequestAsync(request);
        PrintResponse(response);
    }

    private async Task ExampleListTools()
    {
        Console.WriteLine("\n2. List Available Tools");

        var request = new McpRequest
        {
            Id = "2",
            Method = "tools/list"
        };

        var response = await _server.HandleRequestAsync(request);
        PrintResponse(response);
    }

    private async Task ExampleAddUser()
    {
        Console.WriteLine("\n3. Add User (Tool Call)");

        var request = new McpRequest
        {
            Id = "3",
            Method = "tools/call",
            Params = new Dictionary<string, object>
            {
                { "name", "add_user" },
                {
                    "arguments", new Dictionary<string, object>
                    {
                        { "name", "John Doe" },
                        { "email", "john.doe@example.com" }
                    }
                }
            }
        };

        var response = await _server.HandleRequestAsync(request);
        PrintResponse(response);
    }

    private async Task ExampleListUsers()
    {
        Console.WriteLine("\n4. List All Users (Tool Call)");

        var request = new McpRequest
        {
            Id = "4",
            Method = "tools/call",
            Params = new Dictionary<string, object>
            {
                { "name", "list_users" }
            }
        };

        var response = await _server.HandleRequestAsync(request);
        PrintResponse(response);
    }

    private async Task ExampleGetUser()
    {
        Console.WriteLine("\n5. Get User by Name (Tool Call)");

        var request = new McpRequest
        {
            Id = "5",
            Method = "tools/call",
            Params = new Dictionary<string, object>
            {
                { "name", "get_user" },
                {
                    "arguments", new Dictionary<string, object>
                    {
                        { "name", "John Doe" }
                    }
                }
            }
        };

        var response = await _server.HandleRequestAsync(request);
        PrintResponse(response);
    }

    private async Task ExampleUpdateUser()
    {
        Console.WriteLine("\n6. Update User (Tool Call)");

        var request = new McpRequest
        {
            Id = "6",
            Method = "tools/call",
            Params = new Dictionary<string, object>
            {
                { "name", "update_user" },
                {
                    "arguments", new Dictionary<string, object>
                    {
                        { "id", 1 },
                        { "email", "john.updated@example.com" }
                    }
                }
            }
        };

        var response = await _server.HandleRequestAsync(request);
        PrintResponse(response);
    }

    private async Task ExampleListResources()
    {
        Console.WriteLine("\n7. List Available Resources");

        var request = new McpRequest
        {
            Id = "7",
            Method = "resources/list"
        };

        var response = await _server.HandleRequestAsync(request);
        PrintResponse(response);
    }

    private async Task ExampleReadResource()
    {
        Console.WriteLine("\n8. Read Users Resource");

        var request = new McpRequest
        {
            Id = "8",
            Method = "resources/read",
            Params = new Dictionary<string, object>
            {
                { "uri", "users://all" }
            }
        };

        var response = await _server.HandleRequestAsync(request);
        PrintResponse(response);
    }

    private void PrintResponse(McpResponse response)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Console.WriteLine(JsonSerializer.Serialize(response, options));
    }

    /// <summary>
    /// Main entry point for running the example
    /// </summary>
    public static async Task Main(string[] args)
    {
        await RunExamplesAsync();
    }
}
