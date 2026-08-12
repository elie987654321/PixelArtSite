using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Auth;

public sealed class InvalidCredentialsException : UseCaseException
{
    public InvalidCredentialsException() : base("Invalid username or password.")
    {
    }
}
