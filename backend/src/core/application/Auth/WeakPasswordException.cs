using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Auth;

public sealed class WeakPasswordException : UseCaseException
{
    public WeakPasswordException(string reason) : base(reason)
    {
    }
}
