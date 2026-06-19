namespace PixelArt.Api.Models;

// Structural helpers for a jagged pixel grid (Pixel[][]).
// Used by EF's value comparer to track changes without serializing to JSON.
public static class PixelGrid
{
    public static bool AreEqual(Pixel[][]? a, Pixel[][]? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (ReferenceEquals(a, b)) return true;
        if (a.Length != b.Length) return false;

        for (var y = 0; y < a.Length; y++)
        {
            var rowA = a[y];
            var rowB = b[y];
            if (rowA is null && rowB is null) continue;
            if (rowA is null || rowB is null) return false;
            if (rowA.Length != rowB.Length) return false;

            for (var x = 0; x < rowA.Length; x++)
            {
                var pa = rowA[x];
                var pb = rowB[x];
                if (pa is null && pb is null) continue;
                if (pa is null || pb is null) return false;
                if (pa.R != pb.R || pa.G != pb.G || pa.B != pb.B || pa.A != pb.A) return false;
            }
        }
        return true;
    }

    public static int ComputeHashCode(Pixel[][] grid)
    {
        var hash = new HashCode();
        foreach (var row in grid)
        {
            if (row is null) { hash.Add(0); continue; }
            foreach (var pixel in row)
            {
                if (pixel is null) { hash.Add(0); continue; }
                hash.Add(pixel.R);
                hash.Add(pixel.G);
                hash.Add(pixel.B);
                hash.Add(pixel.A);
            }
        }
        return hash.ToHashCode();
    }

    
    public static Pixel[][] DeepCopy(Pixel[][] grid)
    {
        var copy = new Pixel[grid.Length][];
        for (var y = 0; y < grid.Length; y++)
        {
            var row = grid[y];
            if (row is null) { copy[y] = null!; continue; }

            var rowCopy = new Pixel[row.Length];
            for (var x = 0; x < row.Length; x++)
            {
                var p = row[x];
                rowCopy[x] = p is null
                    ? null!
                    : new Pixel { R = p.R, G = p.G, B = p.B, A = p.A };
            }
            copy[y] = rowCopy;
        }
        return copy;
    }
}
