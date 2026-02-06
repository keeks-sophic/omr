using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Robots.Data;

public sealed class RobotSettingsRepository
{
    private readonly AppDbContext _db;

    public RobotSettingsRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<RobotSettingsReportedSnapshot?> GetLatestReportedAsync(string robotId, CancellationToken ct = default) =>
        _db.RobotSettingsReportedSnapshots.AsNoTracking()
            .Where(x => x.RobotId == robotId)
            .OrderByDescending(x => x.ReceivedAt)
            .FirstOrDefaultAsync(ct);

    public async Task InsertReportedAsync(RobotSettingsReportedSnapshot snapshot, CancellationToken ct = default)
    {
        await _db.RobotSettingsReportedSnapshots.AddAsync(snapshot, ct);
        await _db.SaveChangesAsync(ct);
    }
}

