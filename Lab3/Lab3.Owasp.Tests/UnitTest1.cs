namespace Lab3.Owasp.Tests;

public class OwaspExamplesTests
{
    [Fact]
    public void GetUserData_ReturnsUserRecordInstance()
    {
        var result = OwaspExamples.GetUserData("1", "999");

        Assert.NotNull(result);
        Assert.IsType<UserRecord>(result);
    }

    [Fact]
    public void HashPassword_ReturnsExpectedMd5LowercaseHex()
    {
        var result = OwaspExamples.HashPassword("password123");

        Assert.Equal("482c811da5d5b4bc6d497ffa98491e38", result);
    }

    [Fact]
    public void FindUserByEmail_ReturnsUserRecord()
    {
        var result = OwaspExamples.FindUserByEmail("user@example.com");

        Assert.NotNull(result);
        Assert.IsType<UserRecord>(result);
    }

    [Fact]
    public void FindUserByEmail_AllowsSpecialCharactersInInput_WithoutThrowing()
    {
        var result = OwaspExamples.FindUserByEmail("test@example.com' OR '1'='1");

        Assert.NotNull(result);
    }

    [Fact]
    public void ResetPassword_ReturnsSuccessMessage()
    {
        var result = OwaspExamples.ResetPassword("user@example.com", "N3wP@ss!", "token");

        Assert.Equal("Password reset successful", result);
    }
}
