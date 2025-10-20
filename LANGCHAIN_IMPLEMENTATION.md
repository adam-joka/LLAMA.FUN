# LangChain Implementation Guide

This document explains the new LangChain-based database integration added to the Llama.Fun project.

## Overview

The project now supports **two modes** for connecting Llama3 to the database:

1. **Original Mode** (JSON-based CRUD operations)
2. **LangChain Mode** (Natural language SQL queries) **← NEW**

## What is LangChain?

LangChain is an orchestration framework that simplifies the integration between Large Language Models (LLMs) and external tools like databases. Instead of manually parsing JSON responses, LangChain provides:

- **SQL Agent Pattern**: Automatically generates SQL queries from natural language
- **Safety mechanisms**: Query validation and error recovery
- **Schema awareness**: Automatic database structure understanding
- **Flexibility**: Can handle complex queries beyond predefined CRUD operations

## Architecture Comparison

### Original Mode
```
User Input → Llama3 → JSON Parser (regex) → UserCrudHandler → EF Core → SQLite
```

### LangChain Mode
```
User Question → LangChain SQL Agent → Llama3 (generates SQL) →
Query Validator → EF Core → SQLite → Llama3 (formats response) → User
```

## Files Added/Modified

### New Files
- **`LangChainUserCrudHandler.cs`** - SQL Agent implementation using LangChain.NET

### Modified Files
- **`LLama.Fun.csproj`** - Added LangChain NuGet packages:
  - `LangChain.Core` (v0.17.0)
  - `LangChain.Providers.Ollama` (v0.17.0)
  - `LangChain.Databases.Sqlite` (v0.17.0)

- **`Program.cs`** - Added mode selection at startup and dual-mode support

## How to Use

### Running the Application

1. Make sure Ollama is running with llama3.2 model:
   ```bash
   ollama serve
   ollama pull llama3.2
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Choose your mode:
   ```
   Choose your mode:
     1. Original Mode (JSON-based CRUD operations)
     2. LangChain Mode (Natural language SQL queries)

   Enter mode (1 or 2): 2
   ```

### Example Queries - Original Mode (Mode 1)

```
You: add user named John with email john@example.com
You: list all users
You: find user with id 1
You: update user with id 1 to have email newemail@test.com
You: delete user with id 2
```

### Example Queries - LangChain Mode (Mode 2)

```
You: Show me all users with gmail addresses
You: How many users are in the database?
You: Find users created after 2024
You: List users whose name contains John
You: What are the top 5 most recent users?
You: Show me users with emails ending in .com
```

## Key Differences

| Feature | Original Mode | LangChain Mode |
|---------|---------------|----------------|
| **Query Type** | Predefined CRUD operations | Any SELECT query |
| **Complexity** | Simple operations only | Complex queries (JOINs, aggregations, etc.) |
| **Implementation** | Regex JSON parsing | SQL Agent with LLM |
| **Flexibility** | Limited to 5 operations | Unlimited query possibilities |
| **Safety** | Manual validation | Built-in query validation |
| **Response Format** | LLM explains database result | LLM formats SQL results naturally |

## LangChain SQL Agent Flow

1. **User asks natural language question**
   - Example: "How many users have gmail addresses?"

2. **Agent generates SQL query**
   - LLM receives database schema
   - Generates appropriate SQL query
   - Example: `SELECT COUNT(*) FROM Users WHERE Email LIKE '%@gmail.com%'`

3. **Query validation**
   - Ensures only SELECT statements
   - Blocks dangerous operations (DROP, DELETE, UPDATE, INSERT)

4. **Execute query**
   - Runs against SQLite database via EF Core
   - Returns results as JSON

5. **Format results**
   - LLM converts results to natural language
   - Example: "There are 3 users with Gmail addresses in the database."

## Security Features

The `LangChainUserCrudHandler` includes built-in security:

```csharp
private bool IsQuerySafe(string query)
{
    // Must start with SELECT
    // Blocks: DROP, DELETE, UPDATE, INSERT, ALTER, CREATE, TRUNCATE, EXEC, PRAGMA
}
```

This ensures that only read-only queries are executed, protecting your database from modification through natural language commands.

## Code Structure

### LangChainUserCrudHandler Class

**Key Methods:**

1. **`ProcessQueryAsync(string userQuery)`**
   - Main entry point for LangChain mode
   - Orchestrates the entire SQL agent flow

2. **`GenerateSqlQueryAsync(string userQuery)`**
   - Uses LLM to convert natural language to SQL
   - Includes database schema context

3. **`ExecuteSqlQueryAsync(string sqlQuery)`**
   - Executes SQL using EF Core
   - Falls back to raw SQL for aggregate queries

4. **`FormatResultsAsync(string originalQuestion, string sqlQuery, string results)`**
   - Uses LLM to convert SQL results to natural language

5. **`IsQuerySafe(string query)`**
   - Validates query for security

## Advantages of LangChain Approach

1. **Natural Language Understanding**
   - No need to structure requests in specific formats
   - Ask questions naturally

2. **Complex Queries**
   - Supports aggregations (COUNT, SUM, AVG)
   - Can filter, sort, and join (if multiple tables exist)
   - Regular expressions in LIKE clauses

3. **Error Recovery**
   - LLM can regenerate queries if they fail
   - Better handling of ambiguous requests

4. **Extensibility**
   - Easy to add more tables
   - Schema is automatically included in prompts

## Performance Considerations

**LangChain Mode:**
- Requires 2 LLM calls per query (SQL generation + result formatting)
- Slightly slower than original mode
- More flexible and powerful

**Original Mode:**
- Requires 2 LLM calls (command generation + explanation)
- Faster for simple CRUD operations
- Limited to predefined operations

## Future Enhancements

Potential improvements for LangChain mode:

1. **Query Caching**: Cache common SQL queries
2. **Multi-table Support**: Extend to handle joins across tables
3. **Write Operations**: Add safe INSERT/UPDATE/DELETE with confirmation
4. **Query History**: Track and reuse successful queries
5. **Error Retry Logic**: Automatically retry failed queries with corrections

## Dependencies

```xml
<PackageReference Include="LangChain.Core" Version="0.17.0" />
<PackageReference Include="LangChain.Providers.Ollama" Version="0.17.0" />
<PackageReference Include="LangChain.Databases.Sqlite" Version="0.17.0" />
```

## Resources

- **LangChain.NET GitHub**: https://github.com/tryAGI/LangChain
- **LangChain.NET Documentation**: https://tryagi.github.io/LangChain/
- **Ollama**: https://ollama.ai/

## Troubleshooting

**Problem**: "Could not initialize LangChain"
- **Solution**: Ensure Ollama is running at `http://localhost:11434`

**Problem**: "Query rejected for security reasons"
- **Solution**: LangChain mode only allows SELECT queries. Use Original Mode for data modification.

**Problem**: Build errors with LangChain packages
- **Solution**: Run `dotnet restore` and ensure .NET 9.0 SDK is installed

## Conclusion

The LangChain integration provides a more flexible and natural way to query your database using Llama3. While the original mode remains useful for simple CRUD operations, LangChain mode enables complex queries and a more conversational experience.

Choose the mode that best fits your use case:
- **Original Mode**: Fast, simple CRUD operations
- **LangChain Mode**: Complex queries, natural language interaction
