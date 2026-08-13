using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Auth.Exceptions;

public sealed class InvalidUsernameException : UseCaseException
{
    public InvalidUsernameException() : base("Username cannot contain whitespace.")
    {
    }
}
