using PixelArt.Api.Models;
using Xunit;

namespace PixelArt.Api.Tests;

public class PixelGridTests
{
    private static string[][] SampleGrid() =>
    [
        ["#ff0000ff", "#00ff00ff"],
        ["#0000ffff", "#0a141e28"],
    ];

    [Fact]
    public void AreEqual_BothNull_ReturnsTrue()
    {
        Assert.True(PixelGrid.AreEqual(null, null));
    }

    [Fact]
    public void AreEqual_OneNull_ReturnsFalse()
    {
        Assert.False(PixelGrid.AreEqual(SampleGrid(), null));
        Assert.False(PixelGrid.AreEqual(null, SampleGrid()));
    }

    [Fact]
    public void AreEqual_SameInstance_ReturnsTrue()
    {
        var grid = SampleGrid();
        Assert.True(PixelGrid.AreEqual(grid, grid));
    }

    [Fact]
    public void AreEqual_IdenticalContent_ReturnsTrue()
    {
        Assert.True(PixelGrid.AreEqual(SampleGrid(), SampleGrid()));
    }

    [Fact]
    public void AreEqual_DifferentPixel_ReturnsFalse()
    {
        var other = SampleGrid();
        other[0][0] = "#fe0000ff";
        Assert.False(PixelGrid.AreEqual(SampleGrid(), other));
    }

    [Fact]
    public void AreEqual_DifferentDimensions_ReturnsFalse()
    {
        string[][] small = [["#010203ff"]];
        Assert.False(PixelGrid.AreEqual(SampleGrid(), small));
    }

    [Fact]
    public void DeepCopy_ProducesEqualGrid()
    {
        var original = SampleGrid();
        var copy = PixelGrid.DeepCopy(original);
        Assert.True(PixelGrid.AreEqual(original, copy));
    }

    [Fact]
    public void DeepCopy_MutatingCopy_DoesNotAffectOriginal()
    {
        var original = SampleGrid();
        var copy = PixelGrid.DeepCopy(original);

        copy[0][0] = "#000000ff";

        Assert.Equal("#ff0000ff", original[0][0]);
        Assert.False(PixelGrid.AreEqual(original, copy));
    }

    [Fact]
    public void ComputeHashCode_EqualGrids_ProduceSameHash()
    {
        Assert.Equal(
            PixelGrid.ComputeHashCode(SampleGrid()),
            PixelGrid.ComputeHashCode(SampleGrid()));
    }
}
