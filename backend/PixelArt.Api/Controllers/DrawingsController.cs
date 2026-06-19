using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixelArt.Api.Data;
using PixelArt.Api.Dtos;
using PixelArt.Api.Models;

namespace PixelArt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrawingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DrawingsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/drawings
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Drawing>>> GetAll()
    {
        return await _db.Drawings.ToListAsync();
    }

    // GET: api/drawings/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Drawing>> GetById(int id)
    {
        var drawing = await _db.Drawings.FindAsync(id);
        if (drawing is null) return NotFound();
        return drawing;
    }

    // POST: api/drawings
    [HttpPost]
    public async Task<ActionResult<Drawing>> Create(DrawingCreateDto input)
    {
        var drawing = new Drawing
        {
            Name = input.Name,
            Width = input.Width,
            Height = input.Height,
            Pixels = input.Pixels
        };

        _db.Drawings.Add(drawing);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = drawing.Id }, drawing);
    }

    // PUT: api/drawings/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, DrawingCreateDto input)
    {
        var drawing = await _db.Drawings.FindAsync(id);
        if (drawing is null) return NotFound();

        drawing.Name = input.Name;
        drawing.Width = input.Width;
        drawing.Height = input.Height;
        drawing.Pixels = input.Pixels;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/drawings/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var drawing = await _db.Drawings.FindAsync(id);
        if (drawing is null) return NotFound();

        _db.Drawings.Remove(drawing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
