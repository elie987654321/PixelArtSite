using Microsoft.EntityFrameworkCore;
using PixelArt.Core.Abstraction.Persistence;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.External.Infrastructure.Persistence;

public sealed class DrawingRepository : IDrawingRepository
{
    private readonly AppDbContext _db;

    public DrawingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Drawing>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await _db.Drawings
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<Drawing?> FindAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default) =>
        _db.Drawings.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, cancellationToken);

    public async Task CreateAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        _db.Drawings.Add(drawing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        _db.Drawings.Update(drawing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        _db.Drawings.Remove(drawing);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
