using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface IMapService
{
    IEnumerable<Map> GetAll();
    MapGraphDto? GetGraph(int id);
    Map SaveGraph(MapGraphDto graph);
}
