using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class RobotRepository : IRobotRepository
{
    private readonly ApplicationDbContext _db;

    public RobotRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public IEnumerable<Robot> GetAll() => _db.Robots.ToArray();

    public IEnumerable<Robot> GetAllNoTracking() => _db.Robots.AsNoTracking().ToArray();

    public Robot? FindByName(string name) => _db.Robots.FirstOrDefault(r => r.Name == name);

    public Robot? FindByIp(string ip) => _db.Robots.FirstOrDefault(r => r.Ip == ip);

    public void Add(Robot robot) => _db.Robots.Add(robot);

    public void Update(Robot robot) => _db.Robots.Update(robot);

    public void Save() => _db.SaveChanges();
}

