using PixelArt.Core.Abstraction.Auth;

namespace PixelArt.External.Infrastructure.Auth;

// Keeps the BCrypt package reference confined to this layer.
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
