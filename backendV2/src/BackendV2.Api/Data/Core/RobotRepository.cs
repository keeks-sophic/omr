using System.Collections.Generic;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Core;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Core;

public class RobotRepository
{
    private readonly AppDbContext _db;
    public RobotRepository(AppDbContext db) { _db = db; }
    public Task<List<Robot>> ListAsync() => _db.Robots.ToListAsync();
    public Task<Robot?> GetAsync(string id) => _db.Robots.FirstOrDefaultAsync(x => x.RobotId == id);
}
