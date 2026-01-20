using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Auth.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Auth.Data;

public sealed class UserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<User>> ListAsync(CancellationToken ct = default) =>
        _db.Users.AsNoTracking().OrderBy(x => x.Username).ToListAsync(ct);

    public Task<User?> GetAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var uname = (username ?? string.Empty).Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == uname, ct);
    }
}
