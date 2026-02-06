using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Robots.Data;

public sealed class RobotRepository
{
    private readonly AppDbContext _db;

    public RobotRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Robot>> ListAsync(CancellationToken ct = default) =>
        _db.Robots.AsNoTracking().OrderBy(x => x.RobotId).ToListAsync(ct);

    public Task<Robot?> GetAsync(string robotId, CancellationToken ct = default) =>
        _db.Robots.FirstOrDefaultAsync(x => x.RobotId == robotId, ct);

    public async Task<Robot> EnsureExistsAsync(string robotId, CancellationToken ct = default)
    {
        var robot = await _db.Robots.FirstOrDefaultAsync(x => x.RobotId == robotId, ct);
        if (robot != null) return robot;

        robot = new Robot
        {
            RobotId = robotId,
            DisplayName = robotId,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _db.Robots.AddAsync(robot, ct);
        await _db.SaveChangesAsync(ct);
        return robot;
    }

    public async Task UpdateMetadataAsync(string robotId, string? displayName, bool? isEnabled, string? tagsJson, string? notes, CancellationToken ct = default)
    {
        var robot = await EnsureExistsAsync(robotId, ct);
        if (displayName != null) robot.DisplayName = displayName;
        if (isEnabled.HasValue) robot.IsEnabled = isEnabled.Value;
        if (tagsJson != null) robot.TagsJson = tagsJson;
        if (notes != null) robot.Notes = notes;
        robot.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task TouchLastSeenAsync(string robotId, DateTimeOffset seenAt, CancellationToken ct = default)
    {
        var robot = await EnsureExistsAsync(robotId, ct);
        robot.LastSeenAt = seenAt;
        robot.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

