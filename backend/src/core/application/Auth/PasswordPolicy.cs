using System.Text;
using PixelArt.Core.Application.Auth.Exceptions;

namespace PixelArt.Core.Application.Auth;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;

    public const int MaximumBytes = 72;

    public static void Validate(string password)
    {
        if (password.Length < MinimumLength)
            throw new InvalidPasswordException($"Password must be at least {MinimumLength} characters.");

        if (Encoding.UTF8.GetByteCount(password) > MaximumBytes)
            throw new InvalidPasswordException($"Password must be at most {MaximumBytes} bytes.");
    }
}
