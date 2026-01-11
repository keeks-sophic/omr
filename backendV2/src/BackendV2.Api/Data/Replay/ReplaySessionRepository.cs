using System;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Replay;

public class ReplaySessionRepository
{
    private readonly AppDbContext _db;
    public ReplaySessionRepository(AppDbContext db) { _db = db; }
    public Task<BackendV2.Api.Model.Replay.ReplaySession?> GetAsync(Guid id) => _db.ReplaySessions.FirstOrDefaultAsync(x => x.ReplaySessionId == id);
}
