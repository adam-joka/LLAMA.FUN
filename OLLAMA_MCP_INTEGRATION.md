# Ollama + MCP Integration Guide

This guide shows how to integrate your local Ollama (llama3.2) with MCP tools to create an AI assistant that can manage your user database.

## Overview

The integration allows Ollama to:
- **Call MCP tools** - Execute database operations (add, list, update, delete users)
- **Use function calling** - Automatically determine which tools to call based on user queries
- **Maintain context** - Keep conversation history for multi-turn interactions
- **Access resources** - Read database schema and user lists

## Architecture

```
User Query → Ollama (llama3.2) → Tool Decision → MCP Tools → Database
                ↑                                      ↓
                └────────── Tool Results ──────────────┘
```

## Quick Start

### Option 1: Interactive Chat Mode

Add this to your `Program.cs`:

```csharp
using LLama.Fun.Mcp;

var ollama = new OllamaMcpIntegration();

Console.WriteLine("Chat with AI (type 'exit' to quit):");

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();

    if (input?.ToLower() == "exit") break;

    var response = await ollama.ProcessQueryAsync(input);
    Console.WriteLine($"AI: {response}\n");
}
```

### Option 2: Use the Example Application

Add this to your `Program.cs`:

```csharp
using LLama.Fun.Mcp.Examples;

// Run automated examples
await OllamaMcpExample.RunAutomatedExamplesAsync();

// OR run interactive chat
// await OllamaMcpExample.RunInteractiveChatAsync();
```

Then run:
```bash
dotnet run
```

## Example Queries

The AI can understand natural language and execute database operations:

### Adding Users
```
You: Add a user named Alice with email alice@example.com
AI: User 'Alice' added successfully with ID 1

You: Create a new user Bob Smith, email is bob@company.com
AI: User 'Bob Smith' added successfully with ID 2
```

### Listing Users
```
You: Show me all users
AI: Found 2 user(s):
- ID=1, Name=Alice, Email=alice@example.com, Created=2025-01-15
- ID=2, Name=Bob Smith, Email=bob@company.com, Created=2025-01-15

You: How many users are in the database?
AI: There are 2 users in the database.
```

### Finding Users
```
You: Find user with ID 1
AI: User: ID=1, Name=Alice, Email=alice@example.com, Created=2025-01-15

You: Search for a user named Bob
AI: User: ID=2, Name=Bob Smith, Email=bob@company.com, Created=2025-01-15
```

### Updating Users
```
You: Update user 1's email to alice.new@example.com
AI: User 1 updated successfully

You: Change Alice's email to alice@newdomain.com
AI: User 1 updated successfully
```

### Deleting Users
```
You: Delete user with ID 2
AI: User 'Bob Smith' (ID=2) deleted successfully

You: Remove the user named Alice
AI: User 'Alice' (ID=1) deleted successfully
```

## How It Works

### 1. Tool Registration

When you create an `OllamaMcpIntegration` instance, it automatically:
- Creates an MCP server
- Registers all 5 CRUD tools (add_user, get_user, list_users, update_user, delete_user)
- Converts MCP tool definitions to Ollama's function calling format

### 2. Query Processing

When you call `ProcessQueryAsync(userQuery)`:

1. **User query is sent to Ollama** with tool definitions
2. **Ollama decides** if it needs to call any tools
3. **If tools are needed**, the integration:
   - Extracts tool calls from Ollama's response
   - Executes them via MCP server
   - Sends results back to Ollama
4. **Ollama generates** a natural language response
5. **Response is returned** to the user

### 3. Conversation History

The integration maintains conversation history, allowing:
- Multi-turn conversations
- Context awareness
- Follow-up questions

Example:
```
You: Add a user named John
AI: User 'John' added successfully with ID 1

You: What's his email?
AI: I don't see an email for user John. You can add one by updating the user.

You: Update John's email to john@example.com
AI: User 1 updated successfully
```

## API Reference

### OllamaMcpIntegration Class

#### Constructor

```csharp
public OllamaMcpIntegration(
    string ollamaUrl = "http://localhost:11434",
    string modelName = "llama3.2"
)
```

**Parameters:**
- `ollamaUrl`: Ollama server URL (default: http://localhost:11434)
- `modelName`: Model to use (default: llama3.2)

#### Methods

##### ProcessQueryAsync

```csharp
public async Task<string> ProcessQueryAsync(string userQuery)
```

Process a user query and return the AI response. Automatically handles tool calling.

**Example:**
```csharp
var response = await ollama.ProcessQueryAsync("Add a user named Alice");
```

##### ClearHistory

```csharp
public void ClearHistory()
```

Clear the conversation history. Useful for starting fresh conversations.

**Example:**
```csharp
ollama.ClearHistory();
```

##### GetHistory

```csharp
public List<Dictionary<string, object>> GetHistory()
```

Get the current conversation history.

**Example:**
```csharp
var history = ollama.GetHistory();
foreach (var message in history)
{
    Console.WriteLine($"{message["role"]}: {message["content"]}");
}
```

## Configuration

### Using Different Ollama Models

```csharp
// Use a different model
var ollama = new OllamaMcpIntegration(
    ollamaUrl: "http://localhost:11434",
    modelName: "llama3.1"  // or "mistral", "codellama", etc.
);
```

### Custom Ollama URL

```csharp
// Connect to remote Ollama instance
var ollama = new OllamaMcpIntegration(
    ollamaUrl: "http://192.168.1.100:11434",
    modelName: "llama3.2"
);
```

## Prerequisites

### 1. Install Ollama

Download and install from: https://ollama.ai/

### 2. Pull llama3.2 Model

```bash
ollama pull llama3.2
```

### 3. Verify Ollama is Running

```bash
curl http://localhost:11434/api/tags
```

You should see a list of available models including llama3.2.

## Advanced Usage

### Custom System Prompts

You can extend the integration to use custom system prompts:

```csharp
// In your Program.cs, before calling ProcessQueryAsync
var systemPrompt = @"You are a helpful database administrator assistant.
When users ask about users, always be polite and confirm actions before executing.";

// You would need to modify OllamaMcpIntegration to accept system prompts
// or prepend the system message to the conversation history
```

### Error Handling

```csharp
try
{
    var response = await ollama.ProcessQueryAsync(userQuery);
    Console.WriteLine(response);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Ollama connection error: {ex.Message}");
    Console.WriteLine("Make sure Ollama is running: ollama serve");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Streaming Responses

The current implementation doesn't stream, but you can modify `CallOllamaWithToolsAsync` to support streaming:

```csharp
// Change stream = false to stream = true
var requestBody = new
{
    model = _modelName,
    messages = messages,
    tools = tools,
    stream = true  // Enable streaming
};
```

## Integration with Existing Code

### Add to Your Current Program.cs

You can add Ollama MCP mode to your existing dual-mode application:

```csharp
Console.WriteLine("Select mode:");
Console.WriteLine("1. Original CRUD mode (JSON-based)");
Console.WriteLine("2. LangChain SQL Agent mode");
Console.WriteLine("3. Ollama MCP Chat mode (NEW!)");

var mode = Console.ReadLine();

if (mode == "3")
{
    var ollama = new OllamaMcpIntegration();

    Console.WriteLine("Ollama MCP Chat Mode - Type 'exit' to quit\n");

    while (true)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();

        if (input?.ToLower() == "exit") break;

        var response = await ollama.ProcessQueryAsync(input);
        Console.WriteLine($"AI: {response}\n");
    }
}
```

## Troubleshooting

### "Connection refused" Error

**Problem:** Can't connect to Ollama

**Solution:**
```bash
# Start Ollama server
ollama serve

# Verify it's running
curl http://localhost:11434/api/tags
```

### "Model not found" Error

**Problem:** llama3.2 model not installed

**Solution:**
```bash
ollama pull llama3.2
ollama list  # Verify installation
```

### Tools Not Being Called

**Problem:** Ollama responds directly without using tools

**Possible causes:**
1. Model doesn't support function calling (llama3.2 should support it)
2. Query is too vague

**Solution:**
- Be more specific: "Add a user" → "Add a user named John with email john@example.com"
- Try a different model: `ollama pull llama3.1`

### Slow Responses

**Problem:** Each query takes a long time

**Causes:**
- First query is always slower (model loading)
- Tool calls require multiple LLM rounds

**Solutions:**
- Keep Ollama running: `ollama serve`
- Use GPU if available
- Try a smaller model for faster responses

## Performance Tips

1. **Keep Ollama Running**: Don't restart it for each query
2. **Clear History Periodically**: Large histories slow down responses
3. **Use Specific Queries**: Vague queries may cause multiple tool calls
4. **Consider Model Size**: Smaller models = faster responses (but less capable)

## Next Steps

1. **Add More Tools**: Extend `McpUserToolsAdapter` with new operations
2. **Custom Prompting**: Add system prompts for specific behaviors
3. **Multi-Modal**: Add support for analyzing images or documents
4. **Web Interface**: Create a web UI for the chat interface
5. **RAG Integration**: Add document retrieval for knowledge base

## Comparison with Existing Modes

| Feature | Original Mode | LangChain Mode | Ollama MCP Mode |
|---------|---------------|----------------|-----------------|
| Natural Language | ❌ No (JSON) | ✅ Yes | ✅ Yes |
| SQL Queries | ❌ No | ✅ Yes | ❌ No (uses CRUD) |
| Tool Calling | ❌ No | ❌ No | ✅ Yes |
| Conversation | ❌ No | ❌ No | ✅ Yes |
| Complex Queries | ❌ Limited | ✅ Advanced | ⚠️ Medium |
| Speed | ✅ Fast | ⚠️ Medium | ⚠️ Medium |
| Local LLM | ❌ No | ⚠️ Partial | ✅ Full |

## References

- [Ollama Documentation](https://github.com/ollama/ollama)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [Llama 3.2 Model Card](https://ollama.ai/library/llama3.2)
- [Function Calling Guide](https://github.com/ollama/ollama/blob/main/docs/api.md#chat-request-with-tools)
