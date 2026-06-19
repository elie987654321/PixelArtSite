using PixelArt.Api.Models;
using Xunit;

namespace PixelArt.Api.Tests;

public class PixelGridTests
{
    private static Pixel P(byte r, byte g, byte b, byte a = 255) =>
        new() { R = r, G = g, B = b, A = a };

    private static Pixel[][] SampleGrid() =>
    [
        [P(255, 0, 0), P(0, 255, 0)],
        [P(0, 0, 255), P(10, 20, 30, 40)],
    ];

    // ---- AreEqual ----

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
        other[0][0] = P(254, 0, 0);
        Assert.False(PixelGrid.AreEqual(SampleGrid(), other));
    }

    [Fact]
    public void AreEqual_DifferentAlpha_ReturnsFalse()
    {
        var other = SampleGrid();
        other[1][1] = P(10, 20, 30, 41);
        Assert.False(PixelGrid.AreEqual(SampleGrid(), other));
    }

    [Fact]
    public void AreEqual_DifferentDimensions_ReturnsFalse()
    {
        Pixel[][] small = [[P(1, 2, 3)]];
        Assert.False(PixelGrid.AreEqual(SampleGrid(), small));
    }

    // ---- DeepCopy ----

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

        copy[0][0].R = 1;        // mutate a pixel in the copy
        copy[1][0] = P(9, 9, 9); // and swap a whole cell

        Assert.Equal(255, original[0][0].R);
        Assert.False(PixelGrid.AreEqual(original, copy));
    }

    [Fact]
    public void DeepCopy_AllocatesNewPixelInstances()
    {
        var original = SampleGrid();
        var copy = PixelGrid.DeepCopy(original);
        Assert.NotSame(original[0][0], copy[0][0]);
    }

    // ---- ComputeHashCode ----

    [Fact]
    public void ComputeHashCode_EqualGrids_ProduceSameHash()
    {
        Assert.Equal(
            PixelGrid.ComputeHashCode(SampleGrid()),
            PixelGrid.ComputeHashCode(SampleGrid()));
    }
}
