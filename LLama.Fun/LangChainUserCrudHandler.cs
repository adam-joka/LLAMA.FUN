using LangChain.Providers.Ollama;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace LLama.Fun;

/// <summary>
/// LangChain-based handler for user database operations.
/// Uses SQL Agent pattern with Ollama/Llama3 for natural language database queries.
/// </summary>
public class LangChainUserCrudHandler
{
    private readonly OllamaChatModel _llm;
    private readonly ApplicationDbContext _db;
    private readonly string _databaseSchema;

    public LangChainUserCrudHandler(string ollamaEndpoint = "http://localhost:11434", string modelName = "llama3.2")
    {
        // Initialize Ollama LLM provider
        var provider = new OllamaProvider(ollamaEndpoint);
        _llm = new OllamaChatModel(provider, modelName);

        _db = new ApplicationDbContext();
        _databaseSchema = GetDatabaseSchema();
    }

    /// <summary>
    /// Main entry point - processes natural language queries about the user database
    /// </summary>
    public async Task<string> ProcessQueryAsync(string userQuery)
    {
        try
        {
            // Step 1: Generate SQL query from natural language
            var sqlQuery = await GenerateSqlQueryAsync(userQuery);

            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                return "I couldn't generate a valid SQL query from your request. Please try rephrasing.";
            }

            Console.WriteLine($"[LangChain Generated SQL]: {sqlQuery}");

            // Step 2: Validate the query for safety
            if (!IsQuerySafe(sqlQuery))
            {
                return "Query rejected: Only SELECT statements are allowed for security reasons.";
            }

            // Step 3: Execute the query
            var results = await ExecuteSqlQueryAsync(sqlQuery);

            // Step 4: Format results into natural language response
            var naturalLanguageResponse = await FormatResultsAsync(userQuery, sqlQuery, results);

            return naturalLanguageResponse;
        }
        catch (Exception ex)
        {
            return $"Error processing query: {ex.Message}";
        }
    }

    /// <summary>
    /// Uses LLM to generate SQL query from natural language
    /// </summary>
    private async Task<string> GenerateSqlQueryAsync(string userQuery)
    {
        var prompt = $@"You are a SQL expert working with a SQLite database.

Database Schema:
{_databaseSchema}

User Question: {userQuery}

Generate a SQLite query to answer this question. Return ONLY the SQL query, nothing else. No explanations, no markdown formatting, just the raw SQL query.

Important rules:
- Only generate SELECT statements
- Table name is 'Users' (capital U)
- Column names are: Id, Name, Email, CreatedAt
- Use proper SQLite syntax
- For date operations, remember CreatedAt is stored as TEXT in ISO format";

        var responseBuilder = new StringBuilder();
        await foreach (var response in _llm.GenerateAsync(prompt))
        {
            var lastMessage = response.Messages.LastOrDefault();
            responseBuilder.Append(lastMessage.Content);
        }

        // Clean up the response (remove markdown code blocks if present)
        var sqlQuery = responseBuilder.ToString().Trim();
        sqlQuery = sqlQuery.Replace("```sql", "").Replace("```", "").Trim();

        return sqlQuery;
    }

    /// <summary>
    /// Executes SQL query and returns results as JSON string
    /// </summary>
    private async Task<string> ExecuteSqlQueryAsync(string sqlQuery)
    {
        try
        {
            // Use FromSqlRaw to execute the query
            var users = await _db.Users.FromSqlRaw(sqlQuery).ToListAsync();

            if (users.Count == 0)
            {
                return "[]"; // Empty result set
            }

            // Serialize to JSON
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return json;
        }
        catch
        {
            // If the query is not a simple SELECT *, try a different approach
            // For aggregate queries (COUNT, etc.), we need raw SQL execution
            return await ExecuteRawSqlAsync(sqlQuery);
        }
    }

    /// <summary>
    /// Executes raw SQL for aggregate/custom queries
    /// </summary>
    private async Task<string> ExecuteRawSqlAsync(string sqlQuery)
    {
        using var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sqlQuery;

        using var reader = await command.ExecuteReaderAsync();

        var results = new List<Dictionary<string, object>>();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? (object)"null" : reader.GetValue(i);
            }
            results.Add(row);
        }

        return JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Uses LLM to format query results into natural language
    /// </summary>
    private async Task<string> FormatResultsAsync(string originalQuestion, string sqlQuery, string results)
    {
        var resultsPreview = results.Length > 1000 ? results.Substring(0, 1000) + "..." : results;

        var prompt = $@"You are a helpful assistant explaining database query results to a user.

User's Question: {originalQuestion}
SQL Query Executed: {sqlQuery}
Query Results (JSON): {resultsPreview}

Provide a clear, natural language answer to the user's question based on the results.
- Be conversational and friendly
- Include specific data from the results
- If the results are empty, say so clearly
- Keep the response concise but informative";

        var responseBuilder = new StringBuilder();
        await foreach (var response in _llm.GenerateAsync(prompt))
        {
            var lastMessage = response.Messages.LastOrDefault();
            responseBuilder.Append(lastMessage.Content);
        }

        return responseBuilder.ToString().Trim();
    }

    /// <summary>
    /// Validates that the query is safe to execute (only SELECT statements)
    /// </summary>
    private bool IsQuerySafe(string query)
    {
        var trimmedQuery = query.Trim().ToUpperInvariant();

        // Must start with SELECT
        if (!trimmedQuery.StartsWith("SELECT"))
        {
            return false;
        }

        // Forbidden keywords
        var forbiddenKeywords = new[]
        {
            "DROP", "DELETE", "UPDATE", "INSERT", "ALTER",
            "CREATE", "TRUNCATE", "EXEC", "EXECUTE", "PRAGMA"
        };

        foreach (var keyword in forbiddenKeywords)
        {
            if (trimmedQuery.Contains(keyword))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Generates a description of the database schema for the LLM
    /// </summary>
    private string GetDatabaseSchema()
    {
        var schema = new StringBuilder();
        schema.AppendLine("Table: Users");
        schema.AppendLine("Columns:");
        schema.AppendLine("  - Id (INTEGER, Primary Key) - Unique user identifier");
        schema.AppendLine("  - Name (TEXT) - User's full name");
        schema.AppendLine("  - Email (TEXT) - User's email address");
        schema.AppendLine("  - CreatedAt (TEXT) - Timestamp when user was created (ISO 8601 format)");
        schema.AppendLine();
        schema.AppendLine("Example queries:");
        schema.AppendLine("  - SELECT * FROM Users");
        schema.AppendLine("  - SELECT * FROM Users WHERE Email LIKE '%@gmail.com%'");
        schema.AppendLine("  - SELECT COUNT(*) as count FROM Users");
        schema.AppendLine("  - SELECT * FROM Users WHERE Name LIKE '%John%'");

        return schema.ToString();
    }

    /// <summary>
    /// Cleanup resources
    /// </summary>
    public void Dispose()
    {
        _db?.Dispose();
    }
}
