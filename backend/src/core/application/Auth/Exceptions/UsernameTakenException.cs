using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Auth.Exceptions;

public sealed class UsernameTakenException : UseCaseException
{
    public UsernameTakenException() : base("Username is already taken.")
    {
    }
}
