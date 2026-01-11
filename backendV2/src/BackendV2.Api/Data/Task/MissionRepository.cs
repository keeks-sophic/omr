using System;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Task;

public class MissionRepository
{
    private readonly AppDbContext _db;
    public MissionRepository(AppDbContext db) { _db = db; }
    public Task<BackendV2.Api.Model.Task.Mission?> GetAsync(Guid id) => _db.Missions.FirstOrDefaultAsync(x => x.MissionId == id);
}
