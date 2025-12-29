using backend.Models;

namespace backend.Repositories;

public interface IMapRepository
{
    IEnumerable<Map> GetAll();
    Map? FindByIdWithGraph(int id);
    Map SaveGraph(
        int? id,
        string name,
        IEnumerable<(int id, double x, double y)> nodes,
        IEnumerable<(int id, int startId, int endId, bool twoWay)> paths,
        IEnumerable<(int id, int? pathId, string type, string name, double offset)> points,
        IEnumerable<(int id, int? pathId, string data, double offsetStart)> qrs
    );
}
