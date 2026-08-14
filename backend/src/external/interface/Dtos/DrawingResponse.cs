using PixelArt.Core.Domain.Entities;

namespace PixelArt.External.Interface.Dtos;

public class DrawingResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public string[][] Pixels { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public static DrawingResponse From(Drawing drawing) => new()
    {
        Id = drawing.Id,
        Name = drawing.Name,
        Width = drawing.Width,
        Height = drawing.Height,
        Pixels = drawing.Pixels.ToArray(),
        CreatedAt = drawing.CreatedAt
    };
}
