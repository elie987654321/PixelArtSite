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
        var converter = new ValueConverter<Pixel[][], string>(
            grid => JsonSerializer.Serialize(grid, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<Pixel[][]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<Pixel[]>());

        // EF tracks changes by reference by default; arrays need a value comparer
        // so edits to the grid's contents are detected and saved. Compare and copy
        var comparer = new ValueComparer<Pixel[][]>(
            (a, b) => PixelGrid.AreEqual(a, b),
            grid => PixelGrid.ComputeHashCode(grid),
            grid => PixelGrid.DeepCopy(grid));

        modelBuilder.Entity<Drawing>()
            .Property(d => d.Pixels)
            .HasConversion(converter, comparer);
    }
}
