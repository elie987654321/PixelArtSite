namespace PixelArt.Core.Domain.Entities;

public class User
{
    public const int UsernameMinimumLength = 3;

    public const int UsernameMaximumLength = 50;

    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
