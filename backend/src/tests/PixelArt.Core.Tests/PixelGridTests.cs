using PixelArt.Core.Domain;

namespace PixelArt.Core.Tests;

public class PixelGridTests
{
    private static string[][] SampleRows() =>
    [
        ["#ff0000ff", "#00ff00ff"],
        ["#0000ffff", "#0a141e28"],
    ];

    private static PixelGrid Sample() => new(SampleRows());

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        Assert.False(Sample().Equals(null));
    }

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var grid = Sample();
        Assert.True(grid.Equals(grid));
    }

    [Fact]
    public void Equals_IdenticalContent_ReturnsTrue()
    {
        Assert.True(Sample().Equals(Sample()));
    }

    [Fact]
    public void Equals_DifferentPixel_ReturnsFalse()
    {
        var rows = SampleRows();
        rows[0][0] = "#fe0000ff";

        Assert.False(Sample().Equals(new PixelGrid(rows)));
    }

    [Fact]
    public void Equals_DifferentDimensions_ReturnsFalse()
    {
        Assert.False(Sample().Equals(new PixelGrid([["#010203ff"]])));
    }

    [Fact]
    public void GetHashCode_EqualGrids_ProduceSameHash()
    {
        Assert.Equal(Sample().GetHashCode(), Sample().GetHashCode());
    }

    [Fact]
    public void WidthAndHeight_ReflectTheRows()
    {
        var grid = Sample();

        Assert.Equal(2, grid.Width);
        Assert.Equal(2, grid.Height);
    }

    [Fact]
    public void Empty_HasZeroDimensions()
    {
        Assert.Equal(0, PixelGrid.Empty.Width);
        Assert.Equal(0, PixelGrid.Empty.Height);
    }

    [Fact]
    public void Indexer_ReturnsThePixel()
    {
        Assert.Equal("#0a141e28", Sample()[1, 1]);
    }

    [Fact]
    public void MutatingTheSourceArray_DoesNotAffectTheGrid()
    {
        var rows = SampleRows();
        var grid = new PixelGrid(rows);

        rows[0][0] = "#000000ff";

        Assert.Equal("#ff0000ff", grid[0, 0]);
    }

    [Fact]
    public void MutatingTheReturnedArray_DoesNotAffectTheGrid()
    {
        var grid = Sample();
        var copy = grid.ToArray();

        copy[0][0] = "#000000ff";

        Assert.Equal("#ff0000ff", grid[0, 0]);
    }

    [Fact]
    public void ToArray_RoundTripsThroughAnEqualGrid()
    {
        var grid = Sample();

        Assert.True(grid.Equals(new PixelGrid(grid.ToArray())));
    }
}
