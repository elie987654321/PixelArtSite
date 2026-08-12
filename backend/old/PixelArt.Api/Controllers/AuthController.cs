using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixelArt.Api.Auth;
using PixelArt.Api.Data;
using PixelArt.Api.Dtos;
using PixelArt.Api.Models;

namespace PixelArt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    // POST: api/auth/register
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest input)
    {
        var username = input.Username.Trim();

        // Reject duplicates up front for a clean 409 instead of a DB unique-index error.
        var taken = await _db.Users.AnyAsync(u => u.Username == username);
        if (taken) return Conflict("Username is already taken.");

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            Username = user.Username,
            Token = _tokenService.CreateToken(user)
        });
    }

    // POST: api/auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest input)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == input.Username.Trim());

        if (user is null || !BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
            return Unauthorized("Invalid username or password.");

        return Ok(new AuthResponse
        {
            Username = user.Username,
            Token = _tokenService.CreateToken(user)
        });
    }
}
