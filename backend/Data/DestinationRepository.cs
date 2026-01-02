using Backend.Infrastructure.Persistence;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class DestinationRepository
{
    private readonly AppDbContext _db;
    public DestinationRepository(AppDbContext db) { _db = db; }

    public async Task<Destinations?> GetByRobotIdAsync(int robotId, CancellationToken ct)
    {
        return await _db.Destinations.AsNoTracking().FirstOrDefaultAsync(d => d.RobotId == robotId, ct);
    }

    public async Task<int> UpsertDestinationAsync(int robotId, int mapId, double x, double y, CancellationToken ct)
    {
        var entity = await _db.Destinations.FirstOrDefaultAsync(d => d.RobotId == robotId, ct);
        if (entity == null)
        {
            entity = new Destinations
            {
                RobotId = robotId,
                MapId = mapId,
                X = x,
                Y = y,
                Location = new NetTopologySuite.Geometries.Point(x, y) { SRID = 0 }
            };
            _db.Destinations.Add(entity);
        }
        else
        {
            entity.MapId = mapId;
            entity.X = x;
            entity.Y = y;
            entity.Location = new NetTopologySuite.Geometries.Point(x, y) { SRID = 0 };
        }
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}

