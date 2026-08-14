using PixelArt.Core.Application.Auth.Exceptions;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Application.Auth;

public static class UsernamePolicy
{
    public const int MinimumLength = User.UsernameMinimumLength;

    public const int MaximumLength = User.UsernameMaximumLength;

    public static void Validate(string username)
    {
        if (username.Any(char.IsWhiteSpace))
            throw new InvalidUsernameException("Username cannot contain whitespace.");

        if (username.Length < MinimumLength)
            throw new InvalidUsernameException($"Username must be at least {MinimumLength} characters.");

        if (username.Length > MaximumLength)
            throw new InvalidUsernameException($"Username must be at most {MaximumLength} characters.");
    }
}
