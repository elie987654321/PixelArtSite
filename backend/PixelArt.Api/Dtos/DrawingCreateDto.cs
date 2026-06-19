using PixelArt.Api.Models;

namespace PixelArt.Api.Dtos;

// Input model for creating/updating a Drawing.
// Exposes only the fields a client is allowed to set —
// Id and CreatedAt are controlled by the server, so they are not here.
public class DrawingCreateDto
{
    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public Pixel[][] Pixels { get; set; } = [];
}
