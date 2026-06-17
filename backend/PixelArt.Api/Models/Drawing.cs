namespace PixelArt.Api.Models;

public class Drawing
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    // The pixel grid serialized as JSON (e.g. a 2D array of hex color strings).
    public string PixelData { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
