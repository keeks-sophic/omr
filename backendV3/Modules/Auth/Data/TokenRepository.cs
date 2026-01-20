using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Auth.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Auth.Data;

public sealed class TokenRepository
{
    private readonly AppDbContext _db;

    public TokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default) =>
        _db.RevokedTokens.AsNoTracking().AnyAsync(x => x.Jti == jti, ct);

    public async Task RevokeAsync(Guid? userId, string jti, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var entity = new RevokedToken
        {
            RevocationId = Guid.NewGuid(),
            UserId = userId,
            Jti = jti,
            RevokedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };
        _db.RevokedTokens.Add(entity);
        await _db.SaveChangesAsync(ct);
    }
}

