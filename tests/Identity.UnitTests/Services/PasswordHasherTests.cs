using Identity.Infrastructure.Services;
using Xunit;

namespace Identity.UnitTests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyString()
    {
        var password = "SecurePassword123!";
        var hash = _hasher.HashPassword(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "MyTestPassword@2026";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        var password = "CorrectPassword123";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword("WrongPassword456", hash);

        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse()
    {
        var isValid = _hasher.VerifyPassword("SomePassword", "invalid_base64_or_format");
        Assert.False(isValid);
    }
}
