using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PixelArt.Core.Application.Drawings;
using PixelArt.External.Interface.Dtos;

namespace PixelArt.External.Interface.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrawingsController : ControllerBase
{
    private readonly DrawingService _drawingService;

    public DrawingsController(DrawingService drawingService)
    {
        _drawingService = drawingService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DrawingResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var drawings = await _drawingService.ListAsync(CurrentUserId, cancellationToken);

        return Ok(drawings.Select(DrawingResponse.From));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DrawingResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var drawing = await _drawingService.GetAsync(id, CurrentUserId, cancellationToken);

        return Ok(DrawingResponse.From(drawing));
    }

    [HttpPost]
    public async Task<ActionResult<DrawingResponse>> Create(
        DrawingRequest input,
        CancellationToken cancellationToken)
    {
        var drawing = await _drawingService.CreateAsync(
            input.Name, input.Width, input.Height, input.Pixels, CurrentUserId, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = drawing.Id }, DrawingResponse.From(drawing));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        DrawingRequest input,
        CancellationToken cancellationToken)
    {
        await _drawingService.UpdateAsync(
            id, input.Name, input.Width, input.Height, input.Pixels, CurrentUserId, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _drawingService.DeleteAsync(id, CurrentUserId, cancellationToken);

        return NoContent();
    }
}
