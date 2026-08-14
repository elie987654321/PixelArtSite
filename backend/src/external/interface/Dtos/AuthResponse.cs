namespace PixelArt.External.Interface.Dtos;

// What the client receives after a successful register/login.
public class AuthResponse
{
    public string Username { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
}
