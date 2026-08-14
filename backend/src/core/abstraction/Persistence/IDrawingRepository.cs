using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Abstraction.Persistence;

public interface IDrawingRepository
{
    Task<IReadOnlyList<Drawing>> ListAsync(int userId, CancellationToken cancellationToken = default);

    Task<Drawing?> FindAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task CreateAsync(Drawing drawing, CancellationToken cancellationToken = default);

    Task UpdateAsync(Drawing drawing, CancellationToken cancellationToken = default);

    Task DeleteAsync(Drawing drawing, CancellationToken cancellationToken = default);
}
