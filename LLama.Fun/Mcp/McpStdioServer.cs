using System.Text.Json;

namespace LLama.Fun.Mcp;

/// <summary>
/// MCP Server that communicates via stdio (standard input/output)
/// This is the standard transport for MCP servers
/// </summary>
public class McpStdioServer
{
    private readonly McpServer _server;
    private bool _isRunning;

    public McpStdioServer()
    {
        _server = new McpServer();
        RegisterAllTools();
    }

    /// <summary>
    /// Register all available tools and resources
    /// </summary>
    private void RegisterAllTools()
    {
        McpUserToolsAdapter.RegisterUserTools(_server);
        McpUserToolsAdapter.RegisterUserResources(_server);
    }

    /// <summary>
    /// Start the MCP server and listen for stdin messages
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        Console.Error.WriteLine("MCP Server started. Listening on stdin...");

        try
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                var line = await Console.In.ReadLineAsync();

                if (line == null)
                {
                    // End of stream
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var response = await McpServer.ProcessMessageAsync(line, _server);
                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });

                    await Console.Out.WriteLineAsync(responseJson);
                    await Console.Out.FlushAsync();
                }
                catch (Exception ex)
                {
                    // Send error response
                    var errorResponse = new McpResponse
                    {
                        Error = new McpError
                        {
                            Code = -32603,
                            Message = "Internal error",
                            Data = ex.Message
                        }
                    };

                    var errorJson = JsonSerializer.Serialize(errorResponse);
                    await Console.Out.WriteLineAsync(errorJson);
                    await Console.Out.FlushAsync();

                    Console.Error.WriteLine($"Error processing request: {ex.Message}");
                }
            }
        }
        finally
        {
            Console.Error.WriteLine("MCP Server stopped.");
        }
    }

    /// <summary>
    /// Stop the MCP server
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
    }

    /// <summary>
    /// Entry point for running as standalone MCP server
    /// </summary>
    public static async Task Main(string[] args)
    {
        // Initialize database
        using var context = new ApplicationDbContext();
        await context.Database.EnsureCreatedAsync();

        // Start MCP server
        var server = new McpStdioServer();
        await server.StartAsync();
    }
}
