using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelArt.Api.Models;

namespace PixelArt.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Drawing> Drawings => Set<Drawing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The pixel grid is a jagged Pixel[][]; store it as a JSON string in one column.
        var converter = new ValueConverter<Pixel[][], string>(
            grid => JsonSerializer.Serialize(grid, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<Pixel[][]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<Pixel[]>());

        // EF tracks changes by reference by default; arrays need a value comparer
        // so edits to the grid's contents are detected and saved.
        var comparer = new ValueComparer<Pixel[][]>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null)
                   == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            grid => JsonSerializer.Serialize(grid, (JsonSerializerOptions?)null).GetHashCode(),
            grid => JsonSerializer.Deserialize<Pixel[][]>(
                JsonSerializer.Serialize(grid, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);

        modelBuilder.Entity<Drawing>()
            .Property(d => d.Pixels)
            .HasConversion(converter, comparer);
    }
}
