using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LLama.Fun;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Ollama Llama3.2 Interactive Chat with User Database");
Console.WriteLine("===================================================");
Console.WriteLine("Type 'exit' or 'quit' to end the session");
Console.WriteLine("You can ask me to manage users, like:");
Console.WriteLine("  - 'add user named John with email john@example.com'");
Console.WriteLine("  - 'list all users'");
Console.WriteLine("  - 'find user with id 1'");
Console.WriteLine("  - 'delete user with id 2'");
Console.WriteLine();

// Initialize database
using (var db = new ApplicationDbContext())
{
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine("[Database initialized]\n");
}

// Create HTTP client
using var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11434"),
    Timeout = TimeSpan.FromMinutes(5)
};

// Cancellation token source for the loader
CancellationTokenSource? loaderCts = null;

// Conversation history
var messages = new List<object>();

// Add system prompt to help Llama understand its capabilities
messages.Add(new
{
    role = "system",
    content = @"You are an AI assistant with access to a user database. You can help with CRUD operations on users.

When the user asks to perform database operations, respond in the following JSON format:
{
  ""action"": ""database_operation"",
  ""operation"": ""add_user"" | ""get_user"" | ""list_users"" | ""update_user"" | ""delete_user"",
  ""parameters"": { /* operation-specific parameters */ }
}

Examples:
- For 'add user named Adam with email adam@test.com':
{""action"":""database_operation"",""operation"":""add_user"",""parameters"":{""name"":""Adam"",""email"":""adam@test.com""}}

- For 'list all users':
{""action"":""database_operation"",""operation"":""list_users"",""parameters"":{}}

- For 'find user with id 1':
{""action"":""database_operation"",""operation"":""get_user"",""parameters"":{""id"":1}}

After responding with the JSON command, you will receive the result and should explain it to the user in natural language."
});

while (true)
{
    // Get prompt from user
    Console.Write("You: ");
    var prompt = Console.ReadLine();

    // Check for exit commands
    if (string.IsNullOrWhiteSpace(prompt) ||
        prompt.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        prompt.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("\nGoodbye!");
        break;
    }

    // Add user message to conversation history
    messages.Add(new
    {
        role = "user",
        content = prompt
    });

    var requestBody = new
    {
        model = "llama3.2",
        messages = messages,
        stream = false
    };

    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        // Start the loader animation
        loaderCts = new CancellationTokenSource();
        var loaderTask = Task.Run(() => ShowLoader(loaderCts.Token));

        // Call the Ollama Chat API
        var response = await httpClient.PostAsync("/api/chat", content);
        response.EnsureSuccessStatusCode();

        // Parse the response
        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

        // Stop the loader
        loaderCts.Cancel();
        await loaderTask;
        ClearLoader();

        // Extract and display the response
        if (result.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var responseText))
        {
            var assistantResponse = responseText.GetString() ?? string.Empty;

            // Check if response contains a database operation command
            var dbOperationResult = await TryExecuteDatabaseOperation(assistantResponse);

            if (dbOperationResult != null)
            {
                // Database operation was executed
                Console.WriteLine($"[Database operation executed]\n");
                Console.WriteLine($"Result: {dbOperationResult}\n");

                // Add the command and result to conversation history
                messages.Add(new
                {
                    role = "assistant",
                    content = assistantResponse
                });

                // Ask Llama to explain the result to the user
                messages.Add(new
                {
                    role = "user",
                    content = $"The database operation returned: {dbOperationResult}. Please explain this result to the user in natural language."
                });

                // Get explanation from Llama
                var explainRequest = new
                {
                    model = "llama3.2",
                    messages = messages,
                    stream = false
                };

                var explainJson = JsonSerializer.Serialize(explainRequest);
                var explainContent = new StringContent(explainJson, Encoding.UTF8, "application/json");

                loaderCts = new CancellationTokenSource();
                loaderTask = Task.Run(() => ShowLoader(loaderCts.Token));

                var explainResponse = await httpClient.PostAsync("/api/chat", explainContent);
                explainResponse.EnsureSuccessStatusCode();

                var explainResponseJson = await explainResponse.Content.ReadAsStringAsync();
                var explainResult = JsonSerializer.Deserialize<JsonElement>(explainResponseJson);

                loaderCts.Cancel();
                await loaderTask;
                ClearLoader();

                if (explainResult.TryGetProperty("message", out var explainMsg) &&
                    explainMsg.TryGetProperty("content", out var explanation))
                {
                    var explanationText = explanation.GetString();
                    Console.WriteLine($"Llama3.2: {explanationText}");

                    messages.Add(new
                    {
                        role = "assistant",
                        content = explanationText
                    });
                }
            }
            else
            {
                // Regular chat response
                Console.WriteLine($"Llama3.2: {assistantResponse}");

                // Add assistant response to conversation history
                messages.Add(new
                {
                    role = "assistant",
                    content = assistantResponse
                });
            }
        }

        // Display generation time
        if (result.TryGetProperty("total_duration", out var duration))
        {
            var seconds = duration.GetInt64() / 1_000_000_000.0;
            Console.WriteLine($"[Generated in {seconds:F2}s]\n");
        }
    }
    catch (HttpRequestException ex)
    {
        // Stop the loader on error
        if (loaderCts != null)
        {
            loaderCts.Cancel();
            ClearLoader();
        }

        Console.WriteLine($"Error: Could not connect to Ollama at http://localhost:11434");
        Console.WriteLine($"Make sure Ollama is running and llama3.2 model is installed.");
        Console.WriteLine($"Details: {ex.Message}\n");
    }
    catch (Exception ex)
    {
        // Stop the loader on error
        if (loaderCts != null)
        {
            loaderCts.Cancel();
            ClearLoader();
        }

        Console.WriteLine($"Error: {ex.Message}\n");
    }
}

// Loader animation function
static void ShowLoader(CancellationToken cancellationToken)
{
    var spinners = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
    int index = 0;

    Console.Write("\n");

    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"\r{spinners[index]} Thinking...");
            index = (index + 1) % spinners.Length;
            Thread.Sleep(100);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when cancellation is requested
    }
}

// Clear the loader line
static void ClearLoader()
{
    Console.Write("\r" + new string(' ', 20) + "\r");
}

// Try to parse and execute database operation from Llama's response
static async Task<string?> TryExecuteDatabaseOperation(string response)
{
    try
    {
        // Look for JSON object in the response
        var jsonMatch = Regex.Match(response, @"\{[^}]*""action""\s*:\s*""database_operation""[^}]*\}", RegexOptions.Singleline);

        if (!jsonMatch.Success)
            return null;

        var jsonCommand = jsonMatch.Value;
        var commandObj = JsonSerializer.Deserialize<JsonElement>(jsonCommand);

        if (!commandObj.TryGetProperty("operation", out var operationProp))
            return null;

        var operation = operationProp.GetString();
        var parameters = commandObj.TryGetProperty("parameters", out var paramsProp)
            ? paramsProp
            : JsonSerializer.Deserialize<JsonElement>("{}");

        // Execute the database operation
        return await UserCrudHandler.HandleUserOperation(operation!, parameters);
    }
    catch
    {
        // Not a valid database operation command
        return null;
    }
}
