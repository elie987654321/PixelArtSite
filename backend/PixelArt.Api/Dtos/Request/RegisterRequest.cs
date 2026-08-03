namespace PixelArt.Api.Dtos;

// Credentials supplied when creating an account.
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
