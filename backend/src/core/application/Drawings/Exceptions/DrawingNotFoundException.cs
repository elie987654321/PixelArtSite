using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Drawings.Exceptions;

public sealed class DrawingNotFoundException : UseCaseException
{
    public DrawingNotFoundException(int id) : base($"Drawing {id} was not found.")
    {
    }
}
