using backend.Models;

namespace backend.Repositories;

public interface IRobotRepository
{
    IEnumerable<Robot> GetAll();
    IEnumerable<Robot> GetAllNoTracking();
    Robot? FindByName(string name);
    Robot? FindByIp(string ip);
    void Add(Robot robot);
    void Update(Robot robot);
    void Save();
}

