using PixelArt.Core.Application.Drawings.Exceptions;

namespace PixelArt.Core.Application.Drawings;

public static class DrawingPolicy
{
    public const int MaximumNameLength = 100;

    public const int MinimumDimension = 1;

    public const int MaximumDimension = 256;

    public static void Validate(string name, int width, int height, string[][] pixels)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidDrawingException("Name is required.");

        if (name.Length > MaximumNameLength)
            throw new InvalidDrawingException($"Name must be at most {MaximumNameLength} characters.");

        if (width < MinimumDimension || width > MaximumDimension)
            throw new InvalidDrawingException($"Width must be between {MinimumDimension} and {MaximumDimension}.");

        if (height < MinimumDimension || height > MaximumDimension)
            throw new InvalidDrawingException($"Height must be between {MinimumDimension} and {MaximumDimension}.");

        if (pixels.Length != height)
            throw new InvalidDrawingException($"The drawing must contain exactly {height} rows.");

        for (var y = 0; y < pixels.Length; y++)
        {
            var row = pixels[y];

            if (row is null || row.Length != width)
                throw new InvalidDrawingException($"Row {y} must contain exactly {width} pixels.");

            for (var x = 0; x < row.Length; x++)
            {
                if (!IsHexColour(row[x]))
                    throw new InvalidDrawingException($"Pixel at row {y}, column {x} is not a #RRGGBBAA colour.");
            }
        }
    }

    private static bool IsHexColour(string? value)
    {
        if (value is null || value.Length != 9 || value[0] != '#')
            return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!Uri.IsHexDigit(value[i]))
                return false;
        }

        return true;
    }
}
