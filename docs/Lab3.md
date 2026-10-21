# Lab 3 - Spot and fix vulnerabilities

**Goal:** strengthen security awareness in AI-generated code.

**Scenario:** Create a user management system with registration, login, password reset, and profile management.

## Phase 1: AI code generation

Prompt AI for a user manager with:

1. `RegisterUser(email, password, personalInfo)`
2. `AuthenticateUser(email, password)`
3. `ResetPassword(email, newPassword, resetToken)`
4. `UpdateProfile(userId, profileData)`
5. `DeleteUser(userId, confirmPassword)`
6. `GetUserData(userId, requestingUserId)`

## Phase 2: Deliberate vulnerability introduction

`Lab 3/OWASP` contains the pre-seeded snippets for this phase. The weaknesses are intentional and are the review exercise.

**A01 - Broken access control**

```csharp
public static object GetUserData(string userId, string requestingUserId)
{
    // Vulnerable: No authorization check
    return Database.Query($"SELECT * FROM users WHERE id = {userId}");
}
```

**A02 - Cryptographic failures**

```csharp
public static string HashPassword(string password)
{
    // Vulnerable: Weak hashing, no salt
    var hash = MD5.HashData(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(hash).ToLowerInvariant();
}
```

**A03 - Injection**

```csharp
public static UserRecord FindUserByEmail(string email)
{
    // Vulnerable: SQL injection
    var query = $"SELECT * FROM users WHERE email = '{email}'";
    return Database.Execute(query);
}
```

**A07 - Identification and authentication failures**

```csharp
public static string ResetPassword(string email, string newPassword, string resetToken)
{
    // Vulnerable: No token validation, weak session management
    var user = FindUserByEmail(email);
    user.Password = HashPassword(newPassword);
    return "Password reset successful";
}
```

`Lab 3/Exemplar` is a fuller AI-generated user manager with the same class of issues: string-built SQL, MD5 passwords, a hard-coded signing secret, tokens with no expiry, and no check that the caller may read the requested user.

## Phase 3: Security analysis

Review the exemplar by hand, then run a scanner such as SonarQube or the .NET security analyzers. Compare what you found with what the tool found.

## Phase 4: Secure implementation

`Lab 3/Improved` is the reference fix:

- Parameterized SQL
- bcrypt password hashes
- Authorization on user reads
- Signed tokens with an expiry
- Password strength checks
- Redis-backed rate limiting and account lockout

The signing key is a constructor argument. Supply a long random value from configuration.

## Phase 5: Security testing

Write tests that show the exemplar behaviour is no longer possible: injected SQL is treated as data, a caller cannot read another user's record, and a reset without a valid unused token does not change the password.
