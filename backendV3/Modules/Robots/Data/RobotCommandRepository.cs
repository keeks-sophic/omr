using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Robots.Data;

public sealed class RobotCommandRepository
{
    private readonly AppDbContext _db;

    public RobotCommandRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task InsertAsync(RobotCommandLog cmd, CancellationToken ct = default)
    {
        await _db.RobotCommandLogs.AddAsync(cmd, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<RobotCommandLog?> GetAsync(Guid commandId, CancellationToken ct = default) =>
        _db.RobotCommandLogs.FirstOrDefaultAsync(x => x.CommandId == commandId, ct);

    public async Task TouchAckAsync(Guid commandId, DateTimeOffset ackAt, string status, CancellationToken ct = default)
    {
        var cmd = await _db.RobotCommandLogs.FirstOrDefaultAsync(x => x.CommandId == commandId, ct);
        if (cmd == null) return;
        cmd.LastAckAt = ackAt;
        cmd.Status = status;
        await _db.SaveChangesAsync(ct);
    }
}

