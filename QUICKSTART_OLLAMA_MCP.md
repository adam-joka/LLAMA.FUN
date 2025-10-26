# Quick Start: Ollama + MCP Integration

Get your AI assistant with database tools running in 3 steps!

## Step 1: Ensure Ollama is Running

```bash
# Make sure Ollama is installed and running
ollama serve

# In another terminal, pull llama3.2 if you haven't
ollama pull llama3.2
```

## Step 2: Run the Application

The MCP integration is already built into Program.cs. Just run:

```bash
cd LLama.Fun
dotnet run
```

## Step 3: Select MCP Mode

When prompted, enter **3** to use MCP Mode:

```
Ollama Llama3.2 Interactive Chat with User Database
===================================================

Choose your mode:
  1. Original Mode (JSON-based CRUD operations)
  2. LangChain Mode (Natural language SQL queries)
  3. MCP Mode (Model Context Protocol integration)

Enter mode (1, 2, or 3): 3
```

You'll see:
```
[MCP Mode - Model Context Protocol Active]
Natural language database assistant with tool calling:
  - 'Add a user named John with email john@example.com'
  - 'List all users'
  - 'Find user with ID 1'
  - 'Update user 1 email to new@email.com'
  - 'Delete user with ID 2'

Type 'exit' or 'quit' to end the session, 'clear' to clear conversation history

[Database initialized]

[MCP integration initialized]

You:
```

That's it! Start chatting with your database.

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
⠋ Thinking...
Llama3.2: I've successfully added a new user named Alice with the email alice@example.com. The user has been assigned ID 1.

You: Add Bob Smith, email bob@example.com
⠋ Thinking...
Llama3.2: User 'Bob Smith' has been successfully added to the database with ID 2.

You: List all users
⠋ Thinking...
Llama3.2: Here are all the users in the database:
- ID: 1, Name: Alice, Email: alice@example.com, Created: 2025-01-15
- ID: 2, Name: Bob Smith, Email: bob@example.com, Created: 2025-01-15

You: Update Alice's email to alice.new@example.com
⠋ Thinking...
Llama3.2: I've successfully updated Alice's email address to alice.new@example.com.

You: Delete Bob
⠋ Thinking...
Llama3.2: User 'Bob Smith' (ID 2) has been successfully deleted from the database.

You: How many users are left?
⠋ Thinking...
Llama3.2: There is currently 1 user in the database.

You: clear
[History cleared]

You: exit

Goodbye!
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

## Features of Integrated MCP Mode

✅ **No Code Changes Required** - Just select mode 3 when running
✅ **Conversation History** - AI remembers context from previous messages
✅ **Clear Command** - Type 'clear' to reset conversation history
✅ **Animated Loader** - Visual feedback with spinner while processing
✅ **Error Handling** - Graceful error messages if Ollama is unavailable
✅ **Tool Calling** - AI automatically selects the right database operations
✅ **Natural Responses** - Friendly, conversational output

## What's Happening Behind the Scenes?

1. **Your query** goes to Ollama (llama3.2) with available MCP tools
2. **Ollama decides** which database tool to use based on your request
3. **MCP executes** the tool (list_users, create_user, update_user, etc.)
4. **Result goes back** to Ollama with the operation outcome
5. **Ollama generates** a natural language response explaining the result
6. **You get** a friendly answer, and the conversation continues!

## Next Steps

- Read `OLLAMA_MCP_INTEGRATION.md` for full details
- Read `MCP_INTEGRATION_GUIDE.md` for MCP concepts
- Try the interactive mode and have a conversation!
- Extend with your own tools

Enjoy your AI-powered database assistant! 🚀
