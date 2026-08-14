namespace PixelArt.Core.Domain;

public sealed class PixelGrid : IEquatable<PixelGrid>
{
    private readonly string[][] _rows;

    public PixelGrid(string[][] rows)
    {
        _rows = Copy(rows);
    }

    public static PixelGrid Empty { get; } = new([]);

    public int Height => _rows.Length;

    public int Width => _rows.Length == 0 ? 0 : _rows[0]?.Length ?? 0;

    public string this[int y, int x] => _rows[y][x];

    public string[][] ToArray() => Copy(_rows);

    public bool Equals(PixelGrid? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_rows.Length != other._rows.Length) return false;

        for (var y = 0; y < _rows.Length; y++)
        {
            var myRow = _rows[y];
            var theirRow = other._rows[y];

            if (myRow is null && theirRow is null) continue;
            if (myRow is null || theirRow is null) return false;
            if (myRow.Length != theirRow.Length) return false;

            for (var x = 0; x < myRow.Length; x++)
            {
                if (myRow[x] != theirRow[x]) return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as PixelGrid);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var row in _rows)
        {
            if (row is null) { hash.Add(0); continue; }

            foreach (var pixel in row)
            {
                hash.Add(pixel);
            }
        }

        return hash.ToHashCode();
    }

    private static string[][] Copy(string[][] rows)
    {
        var copy = new string[rows.Length][];

        for (var y = 0; y < rows.Length; y++)
        {
            var row = rows[y];
            copy[y] = row is null ? null! : (string[])row.Clone();
        }

        return copy;
    }
}
