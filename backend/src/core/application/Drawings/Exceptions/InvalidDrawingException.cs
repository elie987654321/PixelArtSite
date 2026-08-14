using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Drawings.Exceptions;

public sealed class InvalidDrawingException : UseCaseException
{
    public InvalidDrawingException(string reason) : base(reason)
    {
    }
}
