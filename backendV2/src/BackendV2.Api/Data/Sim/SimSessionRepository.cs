using System;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Sim;

public class SimSessionRepository
{
    private readonly AppDbContext _db;
    public SimSessionRepository(AppDbContext db) { _db = db; }
    public Task<BackendV2.Api.Model.Sim.SimSession?> GetAsync(Guid id) => _db.SimSessions.FirstOrDefaultAsync(x => x.SimSessionId == id);
}
