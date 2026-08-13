using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Auth.Exceptions;

public sealed class InvalidPasswordException : UseCaseException
{
    public InvalidPasswordException(string reason) : base(reason)
    {
    }
}
