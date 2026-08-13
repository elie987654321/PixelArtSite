using PixelArt.Core.Abstraction.Auth;
using PixelArt.Core.Abstraction.Persistence;
using PixelArt.Core.Application.Auth.Exceptions;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Application.Auth;

public sealed class AuthenticationService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthenticationService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthenticatedUser> RegisterAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (username.Any(char.IsWhiteSpace))
            throw new InvalidUsernameException();

        PasswordPolicy.Validate(password);

        if (await _users.UsernameExistsAsync(username, cancellationToken))
            throw new UsernameTakenException();

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(password)
        };

        await _users.CreateAsync(user, cancellationToken);

        return new AuthenticatedUser(user.Username, _tokenService.CreateToken(user));
    }

    public async Task<AuthenticatedUser> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByUsernameAsync(username.Trim(), cancellationToken);

        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return new AuthenticatedUser(user.Username, _tokenService.CreateToken(user));
    }
}
