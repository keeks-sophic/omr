using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Maps.Dto;
using BackendV3.Modules.Maps.Mapping;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Maps.Service;

public sealed class MapSnapshotService
{
    private readonly AppDbContext _db;

    public MapSnapshotService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MapSnapshotDto?> GetSnapshotAsync(Guid mapId, Guid mapVersionId, CancellationToken ct = default)
    {
        var version = await _db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (version == null) return null;

        var nodes = await _db.MapNodes.AsNoTracking().Where(x => x.MapVersionId == mapVersionId).ToListAsync(ct);
        var paths = await _db.MapPaths.AsNoTracking().Where(x => x.MapVersionId == mapVersionId).ToListAsync(ct);
        var points = await _db.MapPoints.AsNoTracking().Where(x => x.MapVersionId == mapVersionId).ToListAsync(ct);
        var qrs = await _db.QrAnchors.AsNoTracking().Where(x => x.MapVersionId == mapVersionId).ToListAsync(ct);

        return new MapSnapshotDto
        {
            Version = MapMappers.ToDto(version),
            Nodes = nodes.Select(MapMappers.ToDto).ToArray(),
            Paths = paths.Select(MapMappers.ToDto).ToArray(),
            Points = points.Select(MapMappers.ToDto).ToArray(),
            Qrs = qrs.Select(MapMappers.ToDto).ToArray()
        };
    }
}
