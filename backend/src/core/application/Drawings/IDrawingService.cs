using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Application.Drawings;

// Drawing use cases, scoped to the owning user.
public interface IDrawingService
{
    Task<IReadOnlyList<Drawing>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Drawing> GetAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default);

    Task<Drawing> CreateAsync(
        string name,
        int width,
        int height,
        string[][] pixels,
        int userId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        string name,
        int width,
        int height,
        string[][] pixels,
        int userId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default);
}
