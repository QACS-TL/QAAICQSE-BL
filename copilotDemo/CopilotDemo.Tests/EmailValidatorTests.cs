namespace CopilotDemo.Tests;

public class EmailValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateEmail_WhenEmailMissing_ReturnsRequiredMessage(string? email)
    {
        var result = EmailValidator.ValidateEmail(email);

        Assert.False(result.IsValid);
        Assert.Equal("Email address is required.", result.Message);
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("missing-at-domain.com")]
    [InlineData("name@domain")]
    public void ValidateEmail_WhenFormatInvalid_ReturnsFormatMessage(string email)
    {
        var result = EmailValidator.ValidateEmail(email);

        Assert.False(result.IsValid);
        Assert.Equal("Invalid email format. Ensure it follows 'example@domain.com'.", result.Message);
    }

    [Fact]
    public void ValidateEmail_WhenContainsConsecutiveDots_ReturnsConsecutiveDotsMessage()
    {
        var result = EmailValidator.ValidateEmail("user..name@domain.com");

        Assert.False(result.IsValid);
        Assert.Equal("Email address contains consecutive dots.", result.Message);
    }

    [Theory]
    [InlineData(".user@domain.com")]
    public void ValidateEmail_WhenStartsOrEndsWithDot_ReturnsDotBoundaryMessage(string email)
    {
        var result = EmailValidator.ValidateEmail(email);

        Assert.False(result.IsValid);
        Assert.Equal("Email address cannot start or end with a dot.", result.Message);
    }

    [Fact]
    public void ValidateEmail_WhenLocalPartExceeds64Characters_ReturnsLocalPartLimitMessage()
    {
        var localPart = new string('a', 65);
        var email = $"{localPart}@domain.com";

        var result = EmailValidator.ValidateEmail(email);

        Assert.False(result.IsValid);
        Assert.Equal("The local part of the email (before '@') exceeds 64 characters.", result.Message);
    }

    [Fact]
    public void ValidateEmail_WhenDomainPartExceeds255Characters_ReturnsDomainPartLimitMessage()
    {
        var domainLabel = new string('b', 252);
        var email = $"a@{domainLabel}.com";

        var result = EmailValidator.ValidateEmail(email);

        Assert.False(result.IsValid);
        Assert.Equal("The domain part of the email (after '@') exceeds 255 characters.", result.Message);
    }

    [Theory]
    [InlineData("user@-domain.com")]
    public void ValidateEmail_WhenDomainStartsOrEndsWithHyphen_ReturnsHyphenBoundaryMessage(string email)
    {
        var result = EmailValidator.ValidateEmail(email);

        Assert.False(result.IsValid);
        Assert.Equal("The domain part cannot start or end with a hyphen.", result.Message);
    }

    [Fact]
    public void ValidateEmail_WhenHyphenIsInsideDomainLabel_ReturnsValidMessage()
    {
        var result = EmailValidator.ValidateEmail("user@domain-.com");

        Assert.True(result.IsValid);
        Assert.Equal("Email address is valid.", result.Message);
    }

    [Theory]
    [InlineData("user@domain.com")]
    [InlineData("user.name+tag@sub.domain.co")]
    [InlineData("USER_123@domain.io")]
    public void ValidateEmail_WhenEmailIsValid_ReturnsValidMessage(string email)
    {
        var result = EmailValidator.ValidateEmail(email);

        Assert.True(result.IsValid);
        Assert.Equal("Email address is valid.", result.Message);
    }
}
