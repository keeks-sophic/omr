using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class MapService : IMapService
{
    private readonly IMapRepository _repo;
    public MapService(IMapRepository repo) { _repo = repo; }

    public IEnumerable<Map> GetAll() => _repo.GetAll();

    public MapGraphDto? GetGraph(int id)
    {
        var map = _repo.FindByIdWithGraph(id);
        if (map is null) return null;
        return new MapGraphDto
        {
            Id = map.Id,
            Name = map.Name,
            Nodes = map.Nodes.Select(n => new MapNodeDto { Id = n.Id, X = n.X, Y = n.Y }).OrderBy(n => n.Id).ToList(),
            Paths = map.Paths.Select(p => new MapPathDto { Id = p.Id, StartNodeId = p.StartNodeId, EndNodeId = p.EndNodeId, TwoWay = p.TwoWay }).OrderBy(p => p.Id).ToList()
        };
    }

    public Map SaveGraph(MapGraphDto graph)
    {
        var map = _repo.SaveGraph(graph.Id == 0 ? null : graph.Id, graph.Name, graph.Nodes.Select(n => (n.Id, n.X, n.Y)), graph.Paths.Select(p => (p.Id, p.StartNodeId, p.EndNodeId, p.TwoWay)));
        return map;
    }
}
