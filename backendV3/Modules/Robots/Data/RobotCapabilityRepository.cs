using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Robots.Data;

public sealed class RobotCapabilityRepository
{
    private readonly AppDbContext _db;

    public RobotCapabilityRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<RobotCapabilitySnapshot?> GetLatestAsync(string robotId, CancellationToken ct = default) =>
        _db.RobotCapabilitySnapshots.AsNoTracking()
            .Where(x => x.RobotId == robotId)
            .OrderByDescending(x => x.ReceivedAt)
            .FirstOrDefaultAsync(ct);

    public async Task InsertAsync(RobotCapabilitySnapshot snapshot, CancellationToken ct = default)
    {
        await _db.RobotCapabilitySnapshots.AddAsync(snapshot, ct);
        await _db.SaveChangesAsync(ct);
    }
}

