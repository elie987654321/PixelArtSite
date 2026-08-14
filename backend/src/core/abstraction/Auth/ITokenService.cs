using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Abstraction.Auth;

// Issues the access token a user receives on register/login. The token format
// (JWT today) is an infrastructure choice and stays behind this port.
public interface ITokenService
{
    string CreateToken(User user);
}
