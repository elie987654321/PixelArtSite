using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelArt.Core.Application.Auth;
using PixelArt.External.Interface.Dtos;

namespace PixelArt.External.Interface.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationService _auth;

    public AuthController(AuthenticationService auth)
    {
        _auth = auth;
    }

    // POST: api/auth/register
    // Failures leave the service as UseCaseException and are turned into
    // ProblemDetails by UseCaseExceptionHandler.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest input,
        CancellationToken cancellationToken)
    {
        var result = await _auth.RegisterAsync(input.Username, input.Password, cancellationToken);

        return Ok(new AuthResponse { Username = result.Username, Token = result.Token });
    }

    // POST: api/auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest input,
        CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(input.Username, input.Password, cancellationToken);

        return Ok(new AuthResponse { Username = result.Username, Token = result.Token });
    }
}
