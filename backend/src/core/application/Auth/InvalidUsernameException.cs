using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Auth;

public sealed class InvalidUsernameException : UseCaseException
{
    public InvalidUsernameException() : base("Username cannot contain whitespace.")
    {
    }
}
