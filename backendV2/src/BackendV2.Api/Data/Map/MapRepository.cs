using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Map;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Map;

public class MapRepository
{
    private readonly AppDbContext _db;
    public MapRepository(AppDbContext db) { _db = db; }
    public Task<List<MapVersion>> ListVersionsAsync() => _db.MapVersions.ToListAsync();
    public Task<MapVersion?> GetVersionAsync(Guid id) => _db.MapVersions.FirstOrDefaultAsync(x => x.MapVersionId == id);
}
