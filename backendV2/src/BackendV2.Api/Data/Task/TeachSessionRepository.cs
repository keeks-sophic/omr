using System;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Task;

public class TeachSessionRepository
{
    private readonly AppDbContext _db;
    public TeachSessionRepository(AppDbContext db) { _db = db; }
    public Task<BackendV2.Api.Model.Task.TeachSession?> GetAsync(Guid id) => _db.TeachSessions.FirstOrDefaultAsync(x => x.TeachSessionId == id);
}
