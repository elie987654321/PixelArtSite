using Microsoft.EntityFrameworkCore;
using PixelArt.Api.Models;

namespace PixelArt.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Drawing> Drawings => Set<Drawing>();
}
