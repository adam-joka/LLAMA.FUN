namespace LLama.Fun.Mcp.Examples;

/// <summary>
/// Example demonstrating Ollama integration with MCP tools
/// </summary>
public class OllamaMcpExample
{
    /// <summary>
    /// Run interactive chat with Ollama using MCP tools
    /// </summary>
    public static async Task RunInteractiveChatAsync()
    {
        Console.WriteLine("=== Ollama + MCP Interactive Chat ===");
        Console.WriteLine("Initializing database...\n");

        // Initialize database
        using var db = new ApplicationDbContext();
        await db.Database.EnsureCreatedAsync();

        // Create Ollama MCP integration
        var ollama = new OllamaMcpIntegration();

        Console.WriteLine("Chat started! The AI can manage users in your database.");
        Console.WriteLine("Try commands like:");
        Console.WriteLine("  - 'Add a user named John with email john@example.com'");
        Console.WriteLine("  - 'List all users'");
        Console.WriteLine("  - 'Find user with ID 1'");
        Console.WriteLine("  - 'Update user 1 email to newemail@example.com'");
        Console.WriteLine("  - 'Delete user with ID 2'");
        Console.WriteLine("\nType 'exit' to quit, 'clear' to clear history\n");

        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.ToLower() == "exit")
                break;

            if (input.ToLower() == "clear")
            {
                ollama.ClearHistory();
                Console.WriteLine("History cleared.\n");
                continue;
            }

            try
            {
                Console.Write("Assistant: ");

                // Show loading indicator
                var cts = new CancellationTokenSource();
                var loadingTask = ShowLoadingIndicatorAsync(cts.Token);

                // Process query
                var response = await ollama.ProcessQueryAsync(input);

                // Stop loading indicator
                cts.Cancel();
                await loadingTask;

                Console.WriteLine(response);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\n");
            }
        }

        Console.WriteLine("Goodbye!");
    }

    /// <summary>
    /// Run automated examples showing Ollama + MCP capabilities
    /// </summary>
    public static async Task RunAutomatedExamplesAsync()
    {
        Console.WriteLine("=== Ollama + MCP Automated Examples ===\n");

        // Initialize database
        using var db = new ApplicationDbContext();
        await db.Database.EnsureCreatedAsync();

        var ollama = new OllamaMcpIntegration();

        var examples = new[]
        {
            "Add a user named Alice Johnson with email alice@example.com",
            "Add another user named Bob Smith with email bob@example.com",
            "List all users in the database",
            "Find the user named Alice",
            "Update Alice's email to alice.johnson@newdomain.com",
            "How many users are in the database?",
            "Delete the user named Bob",
            "Show me all remaining users"
        };

        foreach (var query in examples)
        {
            Console.WriteLine($"User: {query}");
            Console.Write("Assistant: ");

            try
            {
                var cts = new CancellationTokenSource();
                var loadingTask = ShowLoadingIndicatorAsync(cts.Token);

                var response = await ollama.ProcessQueryAsync(query);

                cts.Cancel();
                await loadingTask;

                Console.WriteLine(response);
                Console.WriteLine();

                // Small delay for readability
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\n");
            }
        }

        Console.WriteLine("=== Examples Complete ===");
    }

    /// <summary>
    /// Show animated loading indicator
    /// </summary>
    private static async Task ShowLoadingIndicatorAsync(CancellationToken cancellationToken)
    {
        var spinnerChars = new[] { '|', '/', '-', '\\' };
        var index = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write(spinnerChars[index]);
                index = (index + 1) % spinnerChars.Length;
                await Task.Delay(100, cancellationToken);
                Console.Write("\b");
            }
            Console.Write(" \b"); // Clear the spinner
        }
        catch (TaskCanceledException)
        {
            Console.Write(" \b"); // Clear the spinner
        }
    }

    /// <summary>
    /// Main entry point
    /// </summary>
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--interactive")
        {
            await RunInteractiveChatAsync();
        }
        else
        {
            await RunAutomatedExamplesAsync();
        }
    }
}
