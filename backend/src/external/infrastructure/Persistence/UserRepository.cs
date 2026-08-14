using Microsoft.EntityFrameworkCore;
using PixelArt.Core.Abstraction.Persistence;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.External.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default) =>
        _db.Users.AnyAsync(u => u.Username == username, cancellationToken);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _db.Users.SingleOrDefaultAsync(u => u.Username == username, cancellationToken);

    public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
