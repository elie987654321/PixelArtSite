using PixelArt.Core.Application.Drawings;
using PixelArt.Core.Application.Drawings.Exceptions;

namespace PixelArt.Core.Tests;

public class DrawingPolicyTests
{
    private static string[][] ValidGrid() =>
    [
        ["#FF0000FF", "#00FF00FF"],
        ["#0000FFFF", "#000000FF"],
    ];

    [Fact]
    public void Validate_ValidDrawing_DoesNotThrow()
    {
        Validate("art", 2, 2, ValidGrid());
    }

    [Fact]
    public void Validate_LowercaseHex_IsAccepted()
    {
        string[][] grid = [["#ff0000ff", "#00ff00ff"], ["#0000ffff", "#000000ff"]];
        Validate("art", 2, 2, grid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankName_Throws(string name)
    {
        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate(name, 2, 2, ValidGrid()));

        Assert.Equal("Name is required.", ex.Message);
    }

    [Fact]
    public void Validate_NameTooLong_Throws()
    {
        var name = new string('a', DrawingPolicy.MaximumNameLength + 1);

        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate(name, 2, 2, ValidGrid()));

        Assert.Equal("Name must be at most 100 characters.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(257)]
    public void Validate_WidthOutOfRange_Throws(int width)
    {
        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", width, 2, ValidGrid()));

        Assert.Equal("Width must be between 1 and 256.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(257)]
    public void Validate_HeightOutOfRange_Throws(int height)
    {
        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", 2, height, ValidGrid()));

        Assert.Equal("Height must be between 1 and 256.", ex.Message);
    }

    [Fact]
    public void Validate_RowCountDoesNotMatchHeight_Throws()
    {
        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", 2, 3, ValidGrid()));

        Assert.Equal("The drawing must contain exactly 3 rows.", ex.Message);
    }

    [Fact]
    public void Validate_RowWidthDoesNotMatchWidth_Throws()
    {
        string[][] grid = [["#FF0000FF"], ["#0000FFFF", "#000000FF"]];

        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", 2, 2, grid));

        Assert.Equal("Row 0 must contain exactly 2 pixels.", ex.Message);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#FF0000")]
    [InlineData("#GG0000FF")]
    [InlineData("FF0000FF")]
    public void Validate_MalformedColour_Throws(string colour)
    {
        string[][] grid = [[colour, "#00FF00FF"], ["#0000FFFF", "#000000FF"]];

        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", 2, 2, grid));

        Assert.Equal("Pixel at row 0, column 0 is not a #RRGGBBAA colour.", ex.Message);
    }

    private static void Validate(string name, int width, int height, string[][] pixels) =>
        DrawingPolicy.Validate(name, width, height, pixels);
}
