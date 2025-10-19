# LLama.Fun - AI-Powered User Database Manager

An interactive console application that uses Llama 3.2 (via Ollama) to manage a user database through natural language commands.

## Features

- **Natural Language Interface** - Talk to your database using plain English
- **Full CRUD Operations** - Create, Read, Update, and Delete users
- **Conversation Context** - Maintains chat history for contextual responses
- **SQLite Database** - Lightweight local database using Entity Framework Core
- **Animated Loader** - Visual feedback while Llama processes requests

## Prerequisites

- .NET 9.0 SDK
- [Ollama](https://ollama.ai/) installed and running
- Llama 3.2 model installed in Ollama

## Installation

1. Clone or download this repository

2. Install the Llama 3.2 model in Ollama:
```bash
ollama pull llama3.2
```

3. Make sure Ollama is running:
```bash
ollama serve
```

4. Build the project:
```bash
cd LLama.Fun
dotnet build
```

## Usage

Run the application:
```bash
dotnet run
```

### Example Commands

**Add a user:**
```
You: add user named Adam with email adam.joka@email.com
```

**List all users:**
```
You: list all users
You: show me all the users
```

**Find a specific user:**
```
You: find user with id 1
You: get user named Adam
```

**Update a user:**
```
You: update user 1 with new email newemail@example.com
You: change the name of user 2 to John Smith
```

**Delete a user:**
```
You: delete user with id 2
You: remove the user with id 3
```

**General chat:**
```
You: hello!
You: how many users are in the database?
```

## How It Works

1. **Natural Language Processing** - Your request is sent to Llama 3.2
2. **Command Interpretation** - Llama interprets the request and generates a JSON database command
3. **Database Operation** - The app executes the command using Entity Framework Core
4. **Natural Response** - Llama translates the database result back into conversational language

## Project Structure

```
LLama.Fun/
├── Program.cs              # Main application with chat loop
├── User.cs                 # User entity model
├── ApplicationDbContext.cs # EF Core database context
├── UserCrudHandler.cs      # CRUD operation handlers
└── llama.db               # SQLite database (created on first run)
```

## Database Schema

**Users Table:**
- `Id` (int, primary key) - Auto-incremented user ID
- `Name` (string) - User's name
- `Email` (string) - User's email address
- `CreatedAt` (DateTime) - UTC timestamp of creation

## Configuration

The application connects to Ollama at `http://localhost:11434` by default. To change this, modify the `BaseAddress` in `Program.cs`:

```csharp
using var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11434"),
    Timeout = TimeSpan.FromMinutes(5)
};
```

## Commands

- Type `exit` or `quit` to end the session
- Press Ctrl+C to force quit

## Technologies Used

- **.NET 9.0** - Application framework
- **Entity Framework Core 9.0** - Database ORM
- **SQLite** - Lightweight database engine
- **Ollama API** - Local LLM inference
- **Llama 3.2** - Large language model
- **System.Text.Json** - JSON serialization

## Troubleshooting

**Error: Could not connect to Ollama**
- Make sure Ollama is running (`ollama serve`)
- Verify Ollama is accessible at http://localhost:11434

**Error: Model not found**
- Install Llama 3.2: `ollama pull llama3.2`
- Check installed models: `ollama list`

**Database errors**
- The SQLite database will be created automatically on first run
- Delete `llama.db` to reset the database

## Future Enhancements

- Support for more complex queries (filtering, sorting)
- Additional entities (orders, products, etc.)
- Export data to JSON/CSV
- Multi-user authentication
- Web API interface
- Streaming responses for real-time feedback

## License

MIT License - Feel free to use and modify as needed.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.
