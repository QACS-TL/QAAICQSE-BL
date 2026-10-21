using System.Security.Cryptography;
using System.Text;

namespace Lab3.Owasp;

/// <summary>
/// Pre-seeded vulnerable snippets for Lab 3. The weaknesses are intentional teaching examples.
/// </summary>
public static class OwaspExamples
{
    private static readonly Database Database = new();

    // A01 - Broken Access Control
    public static object GetUserData(string userId, string requestingUserId)
    {
        // Vulnerable: No authorization check
        return Database.Query($"SELECT * FROM users WHERE id = {userId}");
    }

    // A02 - Cryptographic Failures
    public static string HashPassword(string password)
    {
        // Vulnerable: Weak hashing, no salt
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // A03 - Injection
    public static UserRecord FindUserByEmail(string email)
    {
        // Vulnerable: SQL injection
        var query = $"SELECT * FROM users WHERE email = '{email}'";
        return Database.Execute(query);
    }

    // A07 - Identification and Authentication Failures
    public static string ResetPassword(string email, string newPassword, string resetToken)
    {
        // Vulnerable: No token validation, weak session management
        var user = FindUserByEmail(email);
        user.Password = HashPassword(newPassword);
        return "Password reset successful";
    }
}

public sealed class UserRecord
{
    public string? Password { get; set; }
}

/// <summary>
/// Stand-in so the vulnerable snippets compile. It does not represent a safe data API.
/// </summary>
public sealed class Database
{
    public UserRecord Query(string sql) => new();

    public UserRecord Execute(string sql) => new();
}
