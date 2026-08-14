using PixelArt.Core.Abstraction.Persistence;
using PixelArt.Core.Application.Drawings.Exceptions;
using PixelArt.Core.Domain;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Application.Drawings;

public sealed class DrawingService
{
    private readonly IDrawingRepository _drawingRepository;

    public DrawingService(IDrawingRepository drawingRepository)
    {
        _drawingRepository = drawingRepository;
    }

    public Task<IReadOnlyList<Drawing>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        _drawingRepository.ListAsync(userId, cancellationToken);

    public async Task<Drawing> GetAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default) =>
        await _drawingRepository.FindAsync(id, userId, cancellationToken)
            ?? throw new DrawingNotFoundException(id);

    public async Task<Drawing> CreateAsync(
        string name,
        int width,
        int height,
        string[][] pixels,
        int userId,
        CancellationToken cancellationToken = default)
    {
        DrawingPolicy.Validate(name, width, height, pixels);

        var drawing = new Drawing
        {
            Name = name,
            Width = width,
            Height = height,
            Pixels = new PixelGrid(pixels),
            UserId = userId
        };

        await _drawingRepository.CreateAsync(drawing, cancellationToken);

        return drawing;
    }

    public async Task UpdateAsync(
        int id,
        string name,
        int width,
        int height,
        string[][] pixels,
        int userId,
        CancellationToken cancellationToken = default)
    {
        DrawingPolicy.Validate(name, width, height, pixels);

        var drawing = await _drawingRepository.FindAsync(id, userId, cancellationToken)
            ?? throw new DrawingNotFoundException(id);

        drawing.Name = name;
        drawing.Width = width;
        drawing.Height = height;
        drawing.Pixels = new PixelGrid(pixels);

        await _drawingRepository.UpdateAsync(drawing, cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var drawing = await _drawingRepository.FindAsync(id, userId, cancellationToken)
            ?? throw new DrawingNotFoundException(id);

        await _drawingRepository.DeleteAsync(drawing, cancellationToken);
    }
}
