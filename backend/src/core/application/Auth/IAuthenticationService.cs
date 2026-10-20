namespace PixelArt.Core.Application.Auth;

// Register/login use cases, as seen by the interface layer.
public interface IAuthenticationService
{
    Task<AuthenticatedUser> RegisterAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedUser> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
