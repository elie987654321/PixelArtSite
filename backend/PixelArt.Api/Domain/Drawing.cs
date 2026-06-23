namespace PixelArt.Api.Models;

public class Drawing
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    //Each pixel is represented by its hexadecimal value
    public string[][] Pixels { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
