using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Robots.Data;

public sealed class RobotIdentityRepository
{
    private readonly AppDbContext _db;

    public RobotIdentityRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<RobotIdentitySnapshot?> GetLatestAsync(string robotId, CancellationToken ct = default) =>
        _db.RobotIdentitySnapshots.AsNoTracking()
            .Where(x => x.RobotId == robotId)
            .OrderByDescending(x => x.ReceivedAt)
            .FirstOrDefaultAsync(ct);

    public async Task InsertAsync(RobotIdentitySnapshot snapshot, CancellationToken ct = default)
    {
        await _db.RobotIdentitySnapshots.AddAsync(snapshot, ct);
        await _db.SaveChangesAsync(ct);
    }
}

