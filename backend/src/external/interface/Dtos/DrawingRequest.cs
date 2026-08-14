namespace PixelArt.External.Interface.Dtos;

public class DrawingRequest
{
    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public string[][] Pixels { get; set; } = [];
}
