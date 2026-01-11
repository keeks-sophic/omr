using System;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Data.Task;

public class RouteRepository
{
    private readonly AppDbContext _db;
    public RouteRepository(AppDbContext db) { _db = db; }
    public Task<BackendV2.Api.Model.Task.Route?> GetAsync(Guid id) => _db.Routes.FirstOrDefaultAsync(x => x.RouteId == id);
}
