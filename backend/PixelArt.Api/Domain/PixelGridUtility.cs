namespace PixelArt.Api.Models;

// Structural compare/hash/copy for a jagged pixel grid (string[][] of hex colours),
// used by EF's value comparer to track changes without serializing to JSON.
public static class PixelGrid
{
    public static bool AreEqual(string[][]? a, string[][]? b)
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
                if (rowA[x] != rowB[x]) return false;
            }
        }
        return true;
    }

    public static int ComputeHashCode(string[][] grid)
    {
        var hash = new HashCode();
        foreach (var row in grid)
        {
            if (row is null) { hash.Add(0); continue; }
            foreach (var pixel in row)
            {
                hash.Add(pixel);
            }
        }
        return hash.ToHashCode();
    }

    public static string[][] DeepCopy(string[][] grid)
    {
        var copy = new string[grid.Length][];
        for (var y = 0; y < grid.Length; y++)
        {
            var row = grid[y];
            copy[y] = row is null ? null! : (string[])row.Clone();
        }
        return copy;
    }
}
