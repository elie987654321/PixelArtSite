using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Abstraction.Persistence;

public interface IUserRepository
{
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    // Persists the user immediately; the caller gets back a populated Id.
    Task CreateAsync(User user, CancellationToken cancellationToken = default);
}
