using PixelArt.Core.Abstraction.Persistence;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Tests.Drawings;

internal sealed class FakeDrawingRepository : IDrawingRepository
{
    private readonly List<Drawing> _drawings = [];
    private int _nextId = 1;

    public IReadOnlyList<Drawing> Stored => _drawings;

    public Task<IReadOnlyList<Drawing>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Drawing>>(
            _drawings.Where(d => d.UserId == userId).ToList());

    public Task<Drawing?> FindAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_drawings.FirstOrDefault(d => d.Id == id && d.UserId == userId));

    public Task CreateAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        drawing.Id = _nextId++;
        _drawings.Add(drawing);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Drawing drawing, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        _drawings.Remove(drawing);
        return Task.CompletedTask;
    }
}
