using PixelArt.Core.Application.Auth;
using PixelArt.Core.Application.Auth.Exceptions;

namespace PixelArt.Core.Tests;

public class UsernamePolicyTests
{
    [Fact]
    public void Validate_AcceptableUsername_DoesNotThrow()
    {
        UsernamePolicy.Validate("smoketest");
    }

    [Fact]
    public void Validate_AtMaximumLength_DoesNotThrow()
    {
        UsernamePolicy.Validate(new string('a', UsernamePolicy.MaximumLength));
    }

    [Fact]
    public void Validate_AtMinimumLength_DoesNotThrow()
    {
        UsernamePolicy.Validate(new string('a', UsernamePolicy.MinimumLength));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ab")]
    public void Validate_ShorterThanMinimum_Throws(string username)
    {
        var ex = Assert.Throws<InvalidUsernameException>(() => UsernamePolicy.Validate(username));

        Assert.Equal("Username must be at least 3 characters.", ex.Message);
    }

    [Fact]
    public void Validate_LongerThanMaximum_Throws()
    {
        var username = new string('a', UsernamePolicy.MaximumLength + 1);

        var ex = Assert.Throws<InvalidUsernameException>(() => UsernamePolicy.Validate(username));

        Assert.Equal("Username must be at most 50 characters.", ex.Message);
    }

    [Theory]
    [InlineData("a b")]
    [InlineData(" bob")]
    [InlineData("bob ")]
    [InlineData("bo\tb")]
    public void Validate_ContainsWhitespace_Throws(string username)
    {
        var ex = Assert.Throws<InvalidUsernameException>(() => UsernamePolicy.Validate(username));

        Assert.Equal("Username cannot contain whitespace.", ex.Message);
    }
}
