namespace PixelArt.Api.Models;

// A single pixel in a drawing.
// Alpha controls transparency: 0 = fully transparent, 255 = fully opaque.
public class Pixel
{
    public byte R { get; set; }

    public byte G { get; set; }

    public byte B { get; set; }

    public byte A { get; set; } = 255;
}
