using Backend.Infrastructure.Persistence;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database;

public class RobotRepository
{
    private readonly AppDbContext _db;
    public RobotRepository(AppDbContext db) { _db = db; }

    public async Task<List<object>> GetUnassignedRobotsAsync(CancellationToken ct)
    {
        var list = await _db.Robots.AsNoTracking()
            .Where(r => r.MapId == null)
            .Select(r => new
            {
                name = r.Name,
                ip = r.Ip,
                x = r.X,
                y = r.Y,
                state = r.State,
                battery = r.Battery,
                connected = r.Connected,
                lastActive = r.LastActive,
                mapId = r.MapId
            }).ToListAsync(ct);
        return list.Cast<object>().ToList();
    }

    public async Task<Robot?> AssignRobotToMapAsync(string ip, int mapId, CancellationToken ct)
    {
        var rob = await _db.Robots.FirstOrDefaultAsync(r => r.Ip == ip, ct);
        if (rob == null) return null;
        rob.MapId = mapId;
        rob.X = 0;
        rob.Y = 0;
        rob.Location = new NetTopologySuite.Geometries.Point(0, 0) { SRID = 0 };
        await _db.SaveChangesAsync(ct);
        return rob;
    }

    public async Task<Robot?> UnassignRobotAsync(string ip, CancellationToken ct)
    {
        var rob = await _db.Robots.FirstOrDefaultAsync(r => r.Ip == ip, ct);
        if (rob == null) return null;
        rob.MapId = null;
        await _db.SaveChangesAsync(ct);
        return rob;
    }

    public async Task<Robot?> RelocateRobotAsync(string ip, double x, double y, CancellationToken ct)
    {
        var rob = await _db.Robots.FirstOrDefaultAsync(r => r.Ip == ip, ct);
        if (rob == null) return null;
        rob.X = x;
        rob.Y = y;
        rob.Location = new NetTopologySuite.Geometries.Point(x, y) { SRID = 0 };
        await _db.SaveChangesAsync(ct);
        return rob;
    }

    public Task<bool> MoveRobotAsync(string ip, CancellationToken ct)
    {
        return Task.FromResult(true);
    }

    public async Task<List<object>> GetAllRobotsAsync(CancellationToken ct)
    {
        var list = await _db.Robots.AsNoTracking().Select(r => new
        {
            name = r.Name,
            ip = r.Ip,
            x = r.X,
            y = r.Y,
            state = r.State,
            battery = r.Battery,
            connected = r.Connected,
            lastActive = r.LastActive
        }).ToListAsync(ct);
        return list.Cast<object>().ToList();
    }

    public async Task<List<object>> GetRobotsByMapAsync(int mapId, CancellationToken ct)
    {
        var list = await _db.Robots.AsNoTracking()
            .Where(r => r.MapId == mapId)
            .Select(r => new
            {
                name = r.Name,
                ip = r.Ip,
                x = r.X,
                y = r.Y,
                state = r.State,
                battery = r.Battery,
                connected = r.Connected,
                lastActive = r.LastActive,
                mapId = r.MapId
            }).ToListAsync(ct);
        return list.Cast<object>().ToList();
    }
    public async Task<Robot> UpsertRobotTelemetryAsync(string ip, string? name, double? x, double? y, double battery, string? state, int? mapId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var rob = await _db.Robots.FirstOrDefaultAsync(r => r.Ip == ip, ct);
        if (rob == null)
        {
            rob = new Robot
            {
                Ip = ip,
                Name = name ?? ip,
                X = null,
                Y = null,
                Battery = battery,
                State = state ?? "idle",
                Connected = true,
                LastActive = now,
                MapId = null,
                Location = null
            };
            _db.Robots.Add(rob);
            await _db.SaveChangesAsync(ct);
            return rob;
        }
        // Avoid DB writes unless state changes; heartbeat handled in memory
        if (!string.IsNullOrWhiteSpace(state) && !string.Equals(rob.State, state, StringComparison.OrdinalIgnoreCase))
        {
            rob.State = state!;
            rob.Battery = battery;
            if (x.HasValue) rob.X = x.Value;
            if (y.HasValue) rob.Y = y.Value;
            if (x.HasValue && y.HasValue)
            {
                rob.Location = new NetTopologySuite.Geometries.Point(x.Value, y.Value) { SRID = 0 };
            }
            if (mapId.HasValue) rob.MapId = mapId.Value;
            rob.Connected = true;
            rob.LastActive = now;
            await _db.SaveChangesAsync(ct);
        }
        return rob;
    }

    public async Task MarkRobotDisconnectedAsync(string ip, CancellationToken ct)
    {
        var rob = await _db.Robots.FirstOrDefaultAsync(r => r.Ip == ip, ct);
        if (rob != null && rob.Connected)
        {
            rob.Connected = false;
            await _db.SaveChangesAsync(ct);
        }
    }
}
