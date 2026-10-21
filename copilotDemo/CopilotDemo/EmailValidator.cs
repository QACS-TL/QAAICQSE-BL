using System.Text.RegularExpressions;

namespace CopilotDemo;

public static class EmailValidator
{
    /// <summary>
    /// Validates an email address and returns a detailed message.
    /// </summary>
    /// <returns>Whether the address is valid, and a message explaining the result.</returns>
    public static (bool IsValid, string Message) ValidateEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return (false, "Email address is required.");
        }

        const string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        if (!Regex.IsMatch(email, emailPattern))
        {
            return (false, "Invalid email format. Ensure it follows 'example@domain.com'.");
        }

        if (email.Contains(".."))
        {
            return (false, "Email address contains consecutive dots.");
        }

        if (email.StartsWith('.') || email.EndsWith('.'))
        {
            return (false, "Email address cannot start or end with a dot.");
        }

        if (!email.Contains('@'))
        {
            return (false, "Email address must contain an '@' symbol.");
        }

        var separator = email.IndexOf('@');
        var localPart = email[..separator];
        var domainPart = email[(separator + 1)..];

        if (localPart.Length > 64)
        {
            return (false, "The local part of the email (before '@') exceeds 64 characters.");
        }

        if (domainPart.Length > 255)
        {
            return (false, "The domain part of the email (after '@') exceeds 255 characters.");
        }

        if (!Regex.IsMatch(domainPart, @"^[a-zA-Z0-9.-]+$"))
        {
            return (false, "The domain part contains invalid characters.");
        }

        if (domainPart.StartsWith('-') || domainPart.EndsWith('-'))
        {
            return (false, "The domain part cannot start or end with a hyphen.");
        }

        return (true, "Email address is valid.");
    }

    public static void Main()
    {
        string[] testEmails =
        {
            "example@domain.com",
            "invalid-email@domain..com"
        };

        foreach (var email in testEmails)
        {
            var (isValid, message) = ValidateEmail(email);
            Console.WriteLine($"Email: {email} - Valid: {isValid}, Message: {message}");
        }
    }
}
