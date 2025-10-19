using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LLama.Fun;

public static class UserCrudHandler
{
    public static async Task<string> HandleUserOperation(string operation, JsonElement parameters)
    {
        using var db = new ApplicationDbContext();

        try
        {
            switch (operation.ToLower())
            {
                case "add_user":
                case "create_user":
                    return await AddUser(db, parameters);

                case "get_user":
                case "find_user":
                    return await GetUser(db, parameters);

                case "list_users":
                case "get_all_users":
                    return await ListUsers(db);

                case "update_user":
                    return await UpdateUser(db, parameters);

                case "delete_user":
                    return await DeleteUser(db, parameters);

                default:
                    return $"Unknown operation: {operation}";
            }
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static async Task<string> AddUser(ApplicationDbContext db, JsonElement parameters)
    {
        var name = parameters.TryGetProperty("name", out var n) ? n.GetString() : "";
        var email = parameters.TryGetProperty("email", out var e) ? e.GetString() : "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            return "Error: Name and email are required";
        }

        var user = new User
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return $"User '{name}' added successfully with ID {user.Id}";
    }

    private static async Task<string> GetUser(ApplicationDbContext db, JsonElement parameters)
    {
        if (parameters.TryGetProperty("id", out var id))
        {
            var user = await db.Users.FindAsync(id.GetInt32());
            if (user == null)
                return $"User with ID {id} not found";

            return $"User: ID={user.Id}, Name={user.Name}, Email={user.Email}, Created={user.CreatedAt:yyyy-MM-dd}";
        }

        if (parameters.TryGetProperty("name", out var name))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Name.Contains(name.GetString()!));
            if (user == null)
                return $"User with name '{name}' not found";

            return $"User: ID={user.Id}, Name={user.Name}, Email={user.Email}, Created={user.CreatedAt:yyyy-MM-dd}";
        }

        return "Error: Please provide either 'id' or 'name' to find a user";
    }

    private static async Task<string> ListUsers(ApplicationDbContext db)
    {
        var users = await db.Users.ToListAsync();

        if (users.Count == 0)
            return "No users in database";

        var result = $"Found {users.Count} user(s):\n";
        foreach (var user in users)
        {
            result += $"- ID={user.Id}, Name={user.Name}, Email={user.Email}, Created={user.CreatedAt:yyyy-MM-dd}\n";
        }

        return result.TrimEnd();
    }

    private static async Task<string> UpdateUser(ApplicationDbContext db, JsonElement parameters)
    {
        if (!parameters.TryGetProperty("id", out var idParam))
        {
            return "Error: User ID is required for update";
        }

        var user = await db.Users.FindAsync(idParam.GetInt32());
        if (user == null)
            return $"User with ID {idParam} not found";

        var updated = false;

        if (parameters.TryGetProperty("name", out var name))
        {
            user.Name = name.GetString()!;
            updated = true;
        }

        if (parameters.TryGetProperty("email", out var email))
        {
            user.Email = email.GetString()!;
            updated = true;
        }

        if (!updated)
            return "Error: No fields to update. Provide 'name' or 'email'";

        await db.SaveChangesAsync();
        return $"User {user.Id} updated successfully";
    }

    private static async Task<string> DeleteUser(ApplicationDbContext db, JsonElement parameters)
    {
        if (!parameters.TryGetProperty("id", out var idParam))
        {
            return "Error: User ID is required for deletion";
        }

        var user = await db.Users.FindAsync(idParam.GetInt32());
        if (user == null)
            return $"User with ID {idParam} not found";

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return $"User '{user.Name}' (ID={user.Id}) deleted successfully";
    }

    public static string GetToolsDefinition()
    {
        return JsonSerializer.Serialize(new object[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "add_user",
                    description = "Add a new user to the database",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", description = "The user's name" },
                            email = new { type = "string", description = "The user's email address" }
                        },
                        required = new[] { "name", "email" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_user",
                    description = "Get a user by ID or name",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "integer", description = "The user's ID" },
                            name = new { type = "string", description = "The user's name (partial match)" }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "list_users",
                    description = "List all users in the database",
                    parameters = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "update_user",
                    description = "Update a user's information",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "integer", description = "The user's ID" },
                            name = new { type = "string", description = "New name (optional)" },
                            email = new { type = "string", description = "New email (optional)" }
                        },
                        required = new[] { "id" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "delete_user",
                    description = "Delete a user from the database",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "integer", description = "The user's ID to delete" }
                        },
                        required = new[] { "id" }
                    }
                }
            }
        });
    }
}
