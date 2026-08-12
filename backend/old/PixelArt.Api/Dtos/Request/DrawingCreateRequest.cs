namespace PixelArt.Api.Dtos;

// Input model for creating/updating a Drawing. Pixels[y][x] are #RRGGBBAA hex colours.
public class DrawingCreateDto
{
    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public string[][] Pixels { get; set; } = [];
}
