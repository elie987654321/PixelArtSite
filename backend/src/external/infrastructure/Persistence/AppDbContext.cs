using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelArt.Core.Domain;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.External.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Drawing> Drawings => Set<Drawing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Property(u => u.Username)
            .HasMaxLength(User.UsernameMaximumLength);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Drawing>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var converter = new ValueConverter<PixelGrid, string>(
            grid => JsonSerializer.Serialize(grid.ToArray(), (JsonSerializerOptions?)null),
            json => new PixelGrid(JsonSerializer.Deserialize<string[][]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<string[]>()));

        var comparer = new ValueComparer<PixelGrid>(
            (a, b) => a!.Equals(b),
            grid => grid.GetHashCode(),
            grid => grid);

        modelBuilder.Entity<Drawing>()
            .Property(d => d.Pixels)
            .HasConversion(converter, comparer);
    }
}
