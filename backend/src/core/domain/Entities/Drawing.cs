namespace PixelArt.Core.Domain.Entities;

public class Drawing
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public PixelGrid Pixels { get; set; } = PixelGrid.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
}
