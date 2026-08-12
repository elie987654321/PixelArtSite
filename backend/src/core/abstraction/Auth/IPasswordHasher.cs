namespace PixelArt.Core.Abstraction.Auth;

// Hashes and verifies passwords. The algorithm (BCrypt today) is an
// infrastructure choice and stays behind this port.
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
