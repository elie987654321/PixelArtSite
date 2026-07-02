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

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Usernames must be unique so login can resolve a single account.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        var converter = new ValueConverter<string[][], string>(
            grid => JsonSerializer.Serialize(grid, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<string[][]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<string[]>());

        var comparer = new ValueComparer<string[][]>(
            (a, b) => PixelGrid.AreEqual(a, b),
            grid => PixelGrid.ComputeHashCode(grid),
            grid => PixelGrid.DeepCopy(grid));

        modelBuilder.Entity<Drawing>()
            .Property(d => d.Pixels)
            .HasConversion(converter, comparer);
    }
}
