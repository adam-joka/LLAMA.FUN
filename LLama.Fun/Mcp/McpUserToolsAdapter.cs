using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLama.Fun.Mcp;

/// <summary>
/// Adapter that exposes UserCrudHandler operations as MCP tools
/// </summary>
public static class McpUserToolsAdapter
{
    /// <summary>
    /// Register all user management tools with the MCP server
    /// </summary>
    public static void RegisterUserTools(McpServer server)
    {
        // Register add_user tool
        server.RegisterTool(
            name: "add_user",
            description: "Add a new user to the database with name and email",
            inputSchema: new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "User's full name" },
                    email = new { type = "string", description = "User's email address" }
                },
                required = new[] { "name", "email" }
            },
            handler: async (args) => await ExecuteUserOperation("add_user", args)
        );

        // Register get_user tool
        server.RegisterTool(
            name: "get_user",
            description: "Get a user by ID or name from the database",
            inputSchema: new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer", description = "User ID" },
                    name = new { type = "string", description = "User name" }
                }
            },
            handler: async (args) => await ExecuteUserOperation("get_user", args)
        );

        // Register list_users tool
        server.RegisterTool(
            name: "list_users",
            description: "List all users in the database",
            inputSchema: new
            {
                type = "object",
                properties = new { }
            },
            handler: async (args) => await ExecuteUserOperation("list_users", args)
        );

        // Register update_user tool
        server.RegisterTool(
            name: "update_user",
            description: "Update an existing user's information",
            inputSchema: new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer", description = "User ID to update" },
                    name = new { type = "string", description = "New name (optional)" },
                    email = new { type = "string", description = "New email (optional)" }
                },
                required = new[] { "id" }
            },
            handler: async (args) => await ExecuteUserOperation("update_user", args)
        );

        // Register delete_user tool
        server.RegisterTool(
            name: "delete_user",
            description: "Delete a user from the database by ID",
            inputSchema: new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer", description = "User ID to delete" }
                },
                required = new[] { "id" }
            },
            handler: async (args) => await ExecuteUserOperation("delete_user", args)
        );
    }

    /// <summary>
    /// Register user resources with the MCP server
    /// </summary>
    public static void RegisterUserResources(McpServer server)
    {
        // Register users list resource
        server.RegisterResource(
            uri: "users://all",
            name: "All Users",
            description: "Complete list of all users in the database",
            mimeType: "application/json",
            handler: async () =>
            {
                using var db = new ApplicationDbContext();
                var result = await UserCrudHandler.HandleUserOperation("list_users", default, db);
                return result;
            }
        );

        // Register user schema resource
        server.RegisterResource(
            uri: "users://schema",
            name: "User Schema",
            description: "Database schema for users table",
            mimeType: "application/json",
            handler: async () =>
            {
                await Task.CompletedTask;
                return JsonSerializer.Serialize(new
                {
                    table = "Users",
                    columns = new object[]
                    {
                        new { name = "Id", type = "INTEGER", primaryKey = true },
                        new { name = "Name", type = "TEXT", required = true },
                        new { name = "Email", type = "TEXT", required = true, unique = true },
                        new { name = "CreatedAt", type = "DATETIME", required = true }
                    }
                });
            }
        );
    }

    /// <summary>
    /// Execute a user CRUD operation and return MCP-formatted result
    /// </summary>
    private static async Task<McpToolResult> ExecuteUserOperation(string operation, Dictionary<string, object>? arguments)
    {
        try
        {
            // Convert arguments to JsonElement for UserCrudHandler
            JsonElement parameters = default;
            if (arguments != null)
            {
                var json = JsonSerializer.Serialize(arguments);
                using var doc = JsonDocument.Parse(json);
                parameters = doc.RootElement.Clone();
            }

            // Execute the operation
            var result = await UserCrudHandler.HandleUserOperation(operation, parameters);

            // Return success result
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent
                    {
                        Type = "text",
                        Text = result
                    }
                },
                IsError = false
            };
        }
        catch (Exception ex)
        {
            // Return error result
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent
                    {
                        Type = "text",
                        Text = $"Error executing {operation}: {ex.Message}"
                    }
                },
                IsError = true
            };
        }
    }
}
