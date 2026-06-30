namespace PixelArt.Api.Dtos;

// Credentials supplied when logging in.
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
