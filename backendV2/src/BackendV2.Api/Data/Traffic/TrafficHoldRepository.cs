using System;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Traffic;

public class TrafficHoldRepository
{
    private readonly AppDbContext _db;
    public TrafficHoldRepository(AppDbContext db) { _db = db; }
    public Task<BackendV2.Api.Model.Traffic.TrafficHold?> GetAsync(Guid id) => _db.TrafficHolds.FirstOrDefaultAsync(x => x.HoldId == id);
}
