using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Lab3.Improved;

public sealed class User
{
    public long Id { get; init; }
    public required string Email { get; init; }
    public required string PasswordHash { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? LastLogin { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime? LockedUntil { get; init; }
}

public sealed class SecurityError : Exception
{
    public SecurityError(string message) : base(message)
    {
    }
}

/// <summary>
/// Reference user manager for Lab 3: parameterized SQL, bcrypt, authorization, and expiring tokens.
/// </summary>
public sealed class UserManager
{
    private static readonly string[] TimestampFormats = { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "o" };

    private readonly string _dbPath;
    private readonly string _secretKey;
    private readonly IDatabase _redis;
    private readonly int _maxLoginAttempts = 5;
    private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _tokenExpiry = TimeSpan.FromHours(1);
    private readonly TimeSpan _resetTokenExpiry = TimeSpan.FromMinutes(15);

    public UserManager(string dbPath, string secretKey, IDatabase redis)
    {
        AppContext.SetSwitch("Switch.Microsoft.IdentityModel.UnsafeRelaxHmacKeySizeValidation", true);
        _dbPath = dbPath;
        _secretKey = secretKey; // Should be supplied from configuration, not hard-coded by callers of this lab
        _redis = redis;
        InitializeDatabase();
    }

    public Dictionary<string, object> RegisterUser(string email, string password, IReadOnlyDictionary<string, object?>? personalInfo)
    {
        _ = personalInfo;
        if (string.IsNullOrEmpty(email))
        {
            throw new SecurityError("Invalid email");
        }

        if (!IsValidEmail(email))
        {
            throw new SecurityError("Invalid email format");
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new SecurityError("Invalid password");
        }

        if (!IsStrongPassword(password))
        {
            throw new SecurityError("Password does not meet security requirements");
        }

        if (!CheckRateLimit($"register:{email}", 3, 3600))
        {
            throw new SecurityError("Too many registration attempts");
        }

        email = email.ToLowerInvariant().Trim();
        var passwordHash = HashPassword(password);
        long userId = 0;

        try
        {
            UseConnection((connection, transaction) =>
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO users (email, password_hash) VALUES ($email, $hash)";
                command.Parameters.AddWithValue("$email", email);
                command.Parameters.AddWithValue("$hash", passwordHash);
                command.ExecuteNonQuery();

                command.Parameters.Clear();
                command.CommandText = "SELECT last_insert_rowid()";
                userId = (long)(command.ExecuteScalar() ?? 0L);
            });
        }
        catch (SqliteException exception) when (IsUniqueViolation(exception))
        {
            throw new SecurityError("Email already exists");
        }

        Log.Info($"User registered successfully: {email}");
        return new Dictionary<string, object>
        {
            ["user_id"] = userId,
            ["email"] = email,
            ["message"] = "User registered successfully"
        };
    }

    public Dictionary<string, object>? AuthenticateUser(string? email, string? password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            return null;
        }

        if (!CheckRateLimit($"login:{email}", 10, 900))
        {
            throw new SecurityError("Too many login attempts");
        }

        email = email.ToLowerInvariant().Trim();
        UserRow? userRow = null;
        var passwordMatched = false;

        UseConnection((connection, transaction) =>
        {
            userRow = FindUserByEmail(connection, transaction, email);
            if (userRow is null)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                return;
            }

            if (IsUserLocked(userRow))
            {
                throw new SecurityError("Account is temporarily locked");
            }

            if (!VerifyPassword(password, userRow.PasswordHash))
            {
                var newAttempts = userRow.FailedLoginAttempts + 1;
                string? lockedUntil = null;
                if (newAttempts >= _maxLoginAttempts)
                {
                    lockedUntil = FormatTimestamp(DateTime.Now.Add(_lockoutDuration));
                }

                using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = """
                    UPDATE users
                    SET failed_login_attempts = $attempts, locked_until = $lockedUntil
                    WHERE id = $id
                    """;
                update.Parameters.AddWithValue("$attempts", newAttempts);
                update.Parameters.AddWithValue("$lockedUntil", (object?)lockedUntil ?? DBNull.Value);
                update.Parameters.AddWithValue("$id", userRow.Id);
                update.ExecuteNonQuery();
                Log.Warning($"Failed login attempt for {email}. Attempts: {newAttempts}");
                return;
            }

            passwordMatched = true;
            using var success = connection.CreateCommand();
            success.Transaction = transaction;
            success.CommandText = """
                UPDATE users
                SET failed_login_attempts = 0, locked_until = NULL, last_login = $lastLogin
                WHERE id = $id
                """;
            success.Parameters.AddWithValue("$lastLogin", FormatTimestamp(DateTime.Now));
            success.Parameters.AddWithValue("$id", userRow.Id);
            success.ExecuteNonQuery();
        });

        if (userRow is null || !passwordMatched)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var token = CreateToken(new Dictionary<string, object>
        {
            ["user_id"] = userRow.Id,
            ["email"] = userRow.Email,
            ["iat"] = new DateTimeOffset(now).ToUnixTimeSeconds()
        }, now, now.Add(_tokenExpiry));

        Log.Info($"Successful login for {email}");
        return new Dictionary<string, object>
        {
            ["token"] = token,
            ["user"] = new Dictionary<string, object?>
            {
                ["id"] = userRow.Id,
                ["email"] = userRow.Email,
                ["last_login"] = userRow.LastLogin
            }
        };
    }

    public string GenerateResetToken(string email)
    {
        if (!IsValidEmail(email))
        {
            throw new SecurityError("Invalid email");
        }

        if (!CheckRateLimit($"reset:{email}", 3, 3600))
        {
            throw new SecurityError("Too many reset attempts");
        }

        email = email.ToLowerInvariant().Trim();
        string? resetToken = null;
        var userExists = false;

        UseConnection((connection, transaction) =>
        {
            using var lookup = connection.CreateCommand();
            lookup.Transaction = transaction;
            lookup.CommandText = "SELECT id FROM users WHERE email = $email";
            lookup.Parameters.AddWithValue("$email", email);
            var userId = lookup.ExecuteScalar();
            if (userId is null or DBNull)
            {
                Log.Warning($"Password reset requested for non-existent email: {email}");
                return;
            }

            userExists = true;
            resetToken = GenerateUrlSafeToken();
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO reset_tokens (token, user_id) VALUES ($token, $userId)";
            insert.Parameters.AddWithValue("$token", resetToken);
            insert.Parameters.AddWithValue("$userId", userId);
            insert.ExecuteNonQuery();
        });

        if (!userExists || resetToken is null)
        {
            return "If the email exists, a reset link has been sent";
        }

        Log.Info($"Password reset token generated for {email}");
        return resetToken;
    }

    public Dictionary<string, string> ResetPassword(string resetToken, string newPassword)
    {
        if (string.IsNullOrEmpty(resetToken) || string.IsNullOrEmpty(newPassword))
        {
            throw new SecurityError("Invalid reset request");
        }

        if (!IsStrongPassword(newPassword))
        {
            throw new SecurityError("Password does not meet security requirements");
        }

        long userId = 0;
        UseConnection((connection, transaction) =>
        {
            using var lookup = connection.CreateCommand();
            lookup.Transaction = transaction;
            lookup.CommandText = """
                SELECT rt.user_id, rt.created_at, rt.used, u.email
                FROM reset_tokens rt
                JOIN users u ON rt.user_id = u.id
                WHERE rt.token = $token
                """;
            lookup.Parameters.AddWithValue("$token", resetToken);
            using var reader = lookup.ExecuteReader();
            if (!reader.Read())
            {
                throw new SecurityError("Invalid reset token");
            }

            var used = !reader.IsDBNull(2) && reader.GetInt32(2) != 0;
            if (used)
            {
                throw new SecurityError("Reset token already used");
            }

            var createdAt = ParseTimestamp(reader.GetString(1));
            if (DateTime.Now - createdAt > _resetTokenExpiry)
            {
                throw new SecurityError("Reset token expired");
            }

            userId = reader.GetInt64(0);
            reader.Close();

            using var updateUser = connection.CreateCommand();
            updateUser.Transaction = transaction;
            updateUser.CommandText = """
                UPDATE users
                SET password_hash = $hash, failed_login_attempts = 0, locked_until = NULL
                WHERE id = $id
                """;
            updateUser.Parameters.AddWithValue("$hash", HashPassword(newPassword));
            updateUser.Parameters.AddWithValue("$id", userId);
            updateUser.ExecuteNonQuery();

            using var updateToken = connection.CreateCommand();
            updateToken.Transaction = transaction;
            updateToken.CommandText = "UPDATE reset_tokens SET used = 1 WHERE token = $token";
            updateToken.Parameters.AddWithValue("$token", resetToken);
            updateToken.ExecuteNonQuery();
        });

        Log.Info($"Password reset successful for user {userId}");
        return new Dictionary<string, string> { ["message"] = "Password reset successful" };
    }

    public Dictionary<string, object?>? GetUserData(long userId, long requestingUserId)
    {
        if (userId != requestingUserId)
        {
            Log.Warning($"Unauthorized access attempt: user {requestingUserId} tried to access user {userId}");
            throw new SecurityError("Unauthorized access");
        }

        Dictionary<string, object?>? result = null;
        UseConnection((connection, transaction) =>
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT id, email, created_at, last_login FROM users WHERE id = $id";
            command.Parameters.AddWithValue("$id", userId);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return;
            }

            result = new Dictionary<string, object?>
            {
                ["id"] = reader.GetInt64(0),
                ["email"] = reader.GetString(1),
                ["created_at"] = reader.IsDBNull(2) ? null : reader.GetString(2),
                ["last_login"] = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        });

        return result;
    }

    public Dictionary<string, object> VerifyToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SigningKey(),
                ClockSkew = TimeSpan.Zero
            }, out _);

            var payload = new Dictionary<string, object>();
            foreach (var claim in principal.Claims)
            {
                payload[claim.Type] = claim.Value;
            }

            return payload;
        }
        catch (SecurityTokenExpiredException)
        {
            throw new SecurityError("Token expired");
        }
        catch (SecurityTokenException)
        {
            throw new SecurityError("Invalid token");
        }
    }

    private void InitializeDatabase()
    {
        UseConnection((connection, transaction) =>
        {
            using var users = connection.CreateCommand();
            users.Transaction = transaction;
            users.CommandText = """
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    email TEXT UNIQUE NOT NULL,
                    password_hash TEXT NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    last_login TIMESTAMP,
                    failed_login_attempts INTEGER DEFAULT 0,
                    locked_until TIMESTAMP
                )
                """;
            users.ExecuteNonQuery();

            using var tokens = connection.CreateCommand();
            tokens.Transaction = transaction;
            tokens.CommandText = """
                CREATE TABLE IF NOT EXISTS reset_tokens (
                    token TEXT PRIMARY KEY,
                    user_id INTEGER NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    used INTEGER DEFAULT 0,
                    FOREIGN KEY (user_id) REFERENCES users (id)
                )
                """;
            tokens.ExecuteNonQuery();
        });
    }

    private void UseConnection(Action<SqliteConnection, SqliteTransaction> action)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            action(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    }

    private static bool IsStrongPassword(string password)
    {
        if (password.Length < 8)
        {
            return false;
        }

        if (!Regex.IsMatch(password, "[A-Z]"))
        {
            return false;
        }

        if (!Regex.IsMatch(password, "[a-z]"))
        {
            return false;
        }

        if (!Regex.IsMatch(password, @"\d"))
        {
            return false;
        }

        return Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]");
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (Exception)
        {
            try
            {
                BCrypt.Net.BCrypt.Verify("dummy", BCrypt.Net.BCrypt.HashPassword("dummy"));
            }
            catch (Exception)
            {
                // Keep the failure path from surfacing a different error.
            }

            return false;
        }
    }

    private static bool IsUserLocked(UserRow user)
    {
        if (string.IsNullOrEmpty(user.LockedUntil))
        {
            return false;
        }

        return DateTime.Now < ParseTimestamp(user.LockedUntil);
    }

    private bool CheckRateLimit(string identifier, int maxAttempts, int windowSeconds)
    {
        var key = $"rate_limit:{identifier}";
        var currentAttempts = _redis.StringGet(key);
        if (currentAttempts.IsNull)
        {
            _redis.StringSet(key, 1, TimeSpan.FromSeconds(windowSeconds));
            return true;
        }

        if ((int)currentAttempts >= maxAttempts)
        {
            return false;
        }

        _redis.StringIncrement(key);
        return true;
    }

    private string CreateToken(Dictionary<string, object> claims, DateTime notBefore, DateTime expires)
    {
        var tokenClaims = claims.Select(pair => new Claim(pair.Key, Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? "")).ToList();
        var token = new JwtSecurityToken(
            claims: tokenClaims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private SymmetricSecurityKey SigningKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
    }

    private static UserRow? FindUserByEmail(SqliteConnection connection, SqliteTransaction transaction, string email)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT * FROM users WHERE email = $email";
        command.Parameters.AddWithValue("$email", email);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new UserRow
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetString(reader.GetOrdinal("created_at")),
            LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? null : reader.GetString(reader.GetOrdinal("last_login")),
            FailedLoginAttempts = reader.IsDBNull(reader.GetOrdinal("failed_login_attempts")) ? 0 : reader.GetInt32(reader.GetOrdinal("failed_login_attempts")),
            LockedUntil = reader.IsDBNull(reader.GetOrdinal("locked_until")) ? null : reader.GetString(reader.GetOrdinal("locked_until"))
        };
    }

    private static bool IsUniqueViolation(SqliteException exception)
    {
        return exception.SqliteErrorCode == 19 || exception.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatTimestamp(DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static DateTime ParseTimestamp(string value)
    {
        return DateTime.ParseExact(value, TimestampFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces);
    }

    private static string GenerateUrlSafeToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static class Log
    {
        public static void Info(string message) => Console.Error.WriteLine($"INFO: {message}");
        public static void Warning(string message) => Console.Error.WriteLine($"WARNING: {message}");
    }

    private sealed class UserRow
    {
        public long Id { get; init; }
        public required string Email { get; init; }
        public required string PasswordHash { get; init; }
        public string? CreatedAt { get; init; }
        public string? LastLogin { get; init; }
        public int FailedLoginAttempts { get; init; }
        public string? LockedUntil { get; init; }
    }
}
