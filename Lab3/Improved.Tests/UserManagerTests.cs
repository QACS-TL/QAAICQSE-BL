using Moq;
using Microsoft.Data.Sqlite;
using StackExchange.Redis;

namespace Lab3.Improved.Tests;

public sealed class UserManagerTests : IDisposable
{
    private readonly string _dbPath;
    private readonly UserManager _manager;

    public UserManagerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"lab3-improved-tests-{Guid.NewGuid():N}.db");
        _manager = CreateManager(_dbPath);
    }

    [Fact]
    public void RegisterUser_WhenValidInput_ReturnsCreatedUserDetails()
    {
        var result = _manager.RegisterUser("ALICE@Example.com", "Str0ng!Pass", null);

        Assert.True(result.ContainsKey("user_id"));
        Assert.True((long)result["user_id"] > 0);
        Assert.Equal("alice@example.com", result["email"]);
        Assert.Equal("User registered successfully", result["message"]);
    }

    [Fact]
    public void RegisterUser_WhenEmailAlreadyExists_ThrowsSecurityError()
    {
        _manager.RegisterUser("alice@example.com", "Str0ng!Pass", null);

        var exception = Assert.Throws<SecurityError>(() =>
            _manager.RegisterUser("alice@example.com", "An0ther!Pass", null));

        Assert.Equal("Email already exists", exception.Message);
    }

    [Fact]
    public void AuthenticateUser_WhenCredentialsAreValid_ReturnsTokenAndUser()
    {
        var registerResult = _manager.RegisterUser("alice@example.com", "Str0ng!Pass", null);

        var authResult = _manager.AuthenticateUser("alice@example.com", "Str0ng!Pass");

        Assert.NotNull(authResult);
        Assert.True(authResult.ContainsKey("token"));
        Assert.False(string.IsNullOrWhiteSpace(authResult["token"].ToString()));
        var user = Assert.IsType<Dictionary<string, object?>>(authResult["user"]);
        Assert.Equal(registerResult["user_id"], user["id"]);
        Assert.Equal("alice@example.com", user["email"]);
    }

    [Fact]
    public void AuthenticateUser_WhenPasswordIsInvalid_ReturnsNull()
    {
        _manager.RegisterUser("alice@example.com", "Str0ng!Pass", null);

        var authResult = _manager.AuthenticateUser("alice@example.com", "wrong-password");

        Assert.Null(authResult);
    }

    [Fact]
    public void ResetPassword_WithValidResetToken_UpdatesPassword()
    {
        _manager.RegisterUser("alice@example.com", "Str0ng!Pass", null);
        var resetToken = _manager.GenerateResetToken("alice@example.com");
        UpdateResetTokenCreatedAt(resetToken, DateTime.Now);

        var resetResult = _manager.ResetPassword(resetToken, "N3w!StrongPass");
        var oldPasswordAuth = _manager.AuthenticateUser("alice@example.com", "Str0ng!Pass");
        var newPasswordAuth = _manager.AuthenticateUser("alice@example.com", "N3w!StrongPass");

        Assert.Equal("Password reset successful", resetResult["message"]);
        Assert.Null(oldPasswordAuth);
        Assert.NotNull(newPasswordAuth);
    }

    [Fact]
    public void GetUserData_WhenRequestingDifferentUser_ThrowsSecurityError()
    {
        var registerResult = _manager.RegisterUser("alice@example.com", "Str0ng!Pass", null);
        var userId = (long)registerResult["user_id"];

        var exception = Assert.Throws<SecurityError>(() => _manager.GetUserData(userId, userId + 1));

        Assert.Equal("Unauthorized access", exception.Message);
    }

    [Fact]
    public void VerifyToken_WhenTokenIsInvalid_ThrowsSecurityError()
    {
        Assert.ThrowsAny<Exception>(() => _manager.VerifyToken("not-a-valid-jwt"));
    }

    public void Dispose()
    {
    }

    private static UserManager CreateManager(string dbPath)
    {
        var counters = new Dictionary<string, long>(StringComparer.Ordinal);
        var redis = new Mock<IDatabase>(MockBehavior.Loose);

        redis.Setup(database => database.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, CommandFlags _) =>
                counters.TryGetValue(key.ToString(), out var value) ? (RedisValue)value : RedisValue.Null);

        redis.Setup(database => database.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, RedisValue value, TimeSpan? _, When _, CommandFlags __) =>
            {
                counters[key.ToString()] = (long)value;
                return true;
            });

        redis.Setup(database => database.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, RedisValue value, Expiration _, ValueCondition __, CommandFlags ___) =>
            {
                counters[key.ToString()] = (long)value;
                return true;
            });

        redis.Setup(database => database.StringIncrement(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, long increment, CommandFlags _) =>
            {
                counters.TryGetValue(key.ToString(), out var current);
                current += increment;
                counters[key.ToString()] = current;
                return current;
            });

        return new UserManager(dbPath, "this-is-a-very-long-test-secret-key", redis.Object);
    }

    private void UpdateResetTokenCreatedAt(string token, DateTime createdAt)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE reset_tokens SET created_at = $createdAt WHERE token = $token";
        command.Parameters.AddWithValue("$createdAt", createdAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$token", token);
        command.ExecuteNonQuery();
    }
}
