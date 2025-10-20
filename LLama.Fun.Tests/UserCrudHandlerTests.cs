using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LLama.Fun.Tests;

[TestFixture]
public class UserCrudHandlerTests
{
    private ApplicationDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Add User Tests

    [Test]
    public async Task AddUser_WithValidData_ShouldAddUser()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{
            ""name"": ""John Doe"",
            ""email"": ""john@example.com""
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("add_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("added successfully"));
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john@example.com");
        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Name, Is.EqualTo("John Doe"));
    }

    [Test]
    public async Task AddUser_WithDuplicateEmail_ShouldReturnError()
    {
        // Arrange
        _context.Users.Add(new User
        {
            Name = "Existing User",
            Email = "john@example.com"
        });
        await _context.SaveChangesAsync();

        var parameters = JsonDocument.Parse(@"{
            ""name"": ""John Doe"",
            ""email"": ""john@example.com""
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("add_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Error"));
        Assert.That(result, Does.Contain("already exists"));
        var userCount = await _context.Users.CountAsync();
        Assert.That(userCount, Is.EqualTo(1)); // Should still be only 1 user
    }

    [Test]
    public async Task AddUser_WithMissingName_ShouldReturnError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{
            ""email"": ""john@example.com""
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("add_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Error"));
        Assert.That(result, Does.Contain("required"));
    }

    [Test]
    public async Task AddUser_WithMissingEmail_ShouldReturnError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{
            ""name"": ""John Doe""
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("add_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Error"));
        Assert.That(result, Does.Contain("required"));
    }

    #endregion

    #region Get User Tests

    [Test]
    public async Task GetUser_ById_ShouldReturnUser()
    {
        // Arrange
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var parameters = JsonDocument.Parse($@"{{
            ""id"": {user.Id}
        }}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("get_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("John Doe"));
        Assert.That(result, Does.Contain("john@example.com"));
    }

    [Test]
    public async Task GetUser_ByName_ShouldReturnUser()
    {
        // Arrange
        _context.Users.Add(new User
        {
            Name = "John Doe",
            Email = "john@example.com"
        });
        await _context.SaveChangesAsync();

        var parameters = JsonDocument.Parse(@"{
            ""name"": ""John""
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("get_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("John Doe"));
        Assert.That(result, Does.Contain("john@example.com"));
    }

    [Test]
    public async Task GetUser_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{
            ""id"": 999
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("get_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("not found"));
    }

    [Test]
    public async Task GetUser_WithNoParameters_ShouldReturnError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("get_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Error"));
        Assert.That(result, Does.Contain("id") | Does.Contain("name"));
    }

    #endregion

    #region List Users Tests

    [Test]
    public async Task ListUsers_WithMultipleUsers_ShouldReturnAllUsers()
    {
        // Arrange
        _context.Users.AddRange(
            new User { Name = "User 1", Email = "user1@example.com" },
            new User { Name = "User 2", Email = "user2@example.com" },
            new User { Name = "User 3", Email = "user3@example.com" }
        );
        await _context.SaveChangesAsync();

        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("list_users", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Found 3 user(s)"));
        Assert.That(result, Does.Contain("User 1"));
        Assert.That(result, Does.Contain("User 2"));
        Assert.That(result, Does.Contain("User 3"));
    }

    [Test]
    public async Task ListUsers_WithNoUsers_ShouldReturnEmptyMessage()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("list_users", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("No users"));
    }

    #endregion

    #region Update User Tests

    [Test]
    public async Task UpdateUser_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var parameters = JsonDocument.Parse($@"{{
            ""id"": {user.Id},
            ""name"": ""Jane Doe"",
            ""email"": ""jane@example.com""
        }}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("update_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("updated successfully"));
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.That(updatedUser!.Name, Is.EqualTo("Jane Doe"));
        Assert.That(updatedUser.Email, Is.EqualTo("jane@example.com"));
    }

    [Test]
    public async Task UpdateUser_OnlyName_ShouldUpdateOnlyName()
    {
        // Arrange
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var parameters = JsonDocument.Parse($@"{{
            ""id"": {user.Id},
            ""name"": ""Jane Doe""
        }}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("update_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("updated successfully"));
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.That(updatedUser!.Name, Is.EqualTo("Jane Doe"));
        Assert.That(updatedUser.Email, Is.EqualTo("john@example.com")); // Email unchanged
    }

    [Test]
    public async Task UpdateUser_WithInvalidId_ShouldReturnError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{
            ""id"": 999,
            ""name"": ""Jane Doe""
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("update_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("not found"));
    }

    [Test]
    public async Task UpdateUser_WithoutIdParameter_ShouldReturnError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{
            ""name"": ""Jane Doe""
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("update_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Error"));
        Assert.That(result, Does.Contain("required"));
    }

    [Test]
    public async Task UpdateUser_WithNoFieldsToUpdate_ShouldReturnError()
    {
        // Arrange
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var parameters = JsonDocument.Parse($@"{{
            ""id"": {user.Id}
        }}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("update_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Error"));
        Assert.That(result, Does.Contain("No fields to update"));
    }

    #endregion

    #region Delete User Tests

    [Test]
    public async Task DeleteUser_WithValidId_ShouldDeleteUser()
    {
        // Arrange
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var parameters = JsonDocument.Parse($@"{{
            ""id"": {user.Id}
        }}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("delete_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("deleted successfully"));
        var deletedUser = await _context.Users.FindAsync(user.Id);
        Assert.That(deletedUser, Is.Null);
    }

    [Test]
    public async Task DeleteUser_WithInvalidId_ShouldReturnError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{
            ""id"": 999
        }").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("delete_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("not found"));
    }

    [Test]
    public async Task DeleteUser_WithoutIdParameter_ShouldReturnError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("delete_user", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Error"));
        Assert.That(result, Does.Contain("required"));
    }

    #endregion

    #region Unknown Operation Tests

    [Test]
    public async Task HandleUserOperation_WithUnknownOperation_ShouldReturnError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await UserCrudHandler.HandleUserOperation("unknown_operation", parameters, _context);

        // Assert
        Assert.That(result, Does.Contain("Unknown operation"));
    }

    #endregion
}
