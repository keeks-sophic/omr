using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Maps.Data;

public sealed class MapVersionRepository
{
    private readonly AppDbContext _db;

    public MapVersionRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<MapVersion?> GetAsync(Guid mapVersionId, CancellationToken ct = default) =>
        _db.MapVersions.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId, ct);

    public Task<MapVersion?> GetReadonlyAsync(Guid mapVersionId, CancellationToken ct = default) =>
        _db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId, ct);
}

