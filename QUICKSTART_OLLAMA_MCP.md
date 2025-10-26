# Quick Start: Ollama + MCP Integration

Get your AI assistant with database tools running in 3 steps!

## Step 1: Ensure Ollama is Running

```bash
# Make sure Ollama is installed and running
ollama serve

# In another terminal, pull llama3.2 if you haven't
ollama pull llama3.2
```

## Step 2: Add to Your Program.cs

Replace or add to your existing `Program.cs`:

```csharp
using LLama.Fun;
using LLama.Fun.Mcp;
using LLama.Fun.Mcp.Examples;

// Initialize database
using var context = new ApplicationDbContext();
await context.Database.EnsureCreatedAsync();

Console.WriteLine("Choose a mode:");
Console.WriteLine("1. Run automated Ollama MCP examples");
Console.WriteLine("2. Interactive chat with AI");
Console.Write("\nChoice: ");

var choice = Console.ReadLine();

if (choice == "1")
{
    // Automated examples - shows what the AI can do
    await OllamaMcpExample.RunAutomatedExamplesAsync();
}
else
{
    // Interactive chat mode
    var ollama = new OllamaMcpIntegration();

    Console.WriteLine("\n=== AI Database Assistant ===");
    Console.WriteLine("I can help you manage users in the database!");
    Console.WriteLine("Type 'exit' to quit, 'clear' to clear history\n");

    while (true)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input)) continue;
        if (input.ToLower() == "exit") break;

        if (input.ToLower() == "clear")
        {
            ollama.ClearHistory();
            Console.WriteLine("History cleared.\n");
            continue;
        }

        try
        {
            var response = await ollama.ProcessQueryAsync(input);
            Console.WriteLine($"AI: {response}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Is Ollama running? Try: ollama serve\n");
        }
    }
}
```

## Step 3: Run It!

```bash
dotnet run
```

## What You Can Do

### Try These Commands:

**Add Users:**
- "Add a user named Alice with email alice@example.com"
- "Create Bob Smith, email bob@test.com"

**List Users:**
- "Show me all users"
- "How many users are there?"

**Find Users:**
- "Find user with ID 1"
- "Search for Alice"

**Update Users:**
- "Update user 1's email to newemail@example.com"
- "Change Alice's name to Alicia"

**Delete Users:**
- "Delete user with ID 2"
- "Remove Bob from the database"

## Example Session

```
You: Add a user named Alice with email alice@example.com
AI: User 'Alice' added successfully with ID 1

You: Add Bob Smith, email bob@example.com
AI: User 'Bob Smith' added successfully with ID 2

You: List all users
AI: Found 2 user(s):
- ID=1, Name=Alice, Email=alice@example.com, Created=2025-01-15
- ID=2, Name=Bob Smith, Email=bob@example.com, Created=2025-01-15

You: Update Alice's email to alice.new@example.com
AI: User 1 updated successfully

You: Delete Bob
AI: User 'Bob Smith' (ID=2) deleted successfully

You: How many users are left?
AI: There is 1 user in the database.
```

## Troubleshooting

### Can't connect to Ollama?

Make sure it's running:
```bash
ollama serve
```

### Model not found?

Pull llama3.2:
```bash
ollama pull llama3.2
```

### Slow responses?

- First query is always slower (model loading)
- Keep Ollama running between sessions
- Try a smaller model: `ollama pull llama3.1`

## What's Happening Behind the Scenes?

1. **Your query** goes to Ollama (llama3.2)
2. **Ollama decides** which database tool to use
3. **MCP executes** the tool (add_user, list_users, etc.)
4. **Result goes back** to Ollama
5. **Ollama generates** a natural language response
6. **You get** a friendly answer!

## Next Steps

- Read `OLLAMA_MCP_INTEGRATION.md` for full details
- Read `MCP_INTEGRATION_GUIDE.md` for MCP concepts
- Try the interactive mode and have a conversation!
- Extend with your own tools

Enjoy your AI-powered database assistant! 🚀
