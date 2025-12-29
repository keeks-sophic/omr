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
            Paths = map.Paths.Select(p => new MapPathDto { Id = p.Id, StartNodeId = p.StartNodeId, EndNodeId = p.EndNodeId, TwoWay = p.TwoWay }).OrderBy(p => p.Id).ToList(),
            Points = map.MapPoints.Select(pt => new MapPointDto { Id = pt.Id, PathId = pt.PathId, Type = pt.Type, Name = pt.Name, Offset = pt.Offset }).OrderBy(pt => pt.Id).ToList(),
            Qrs = map.Qrs.Select(q => new QrDto { Id = q.Id, PathId = q.PathId, Data = q.Data, OffsetStart = q.OffsetStart }).OrderBy(q => q.Id).ToList()
        };
    }

    public Map SaveGraph(MapGraphDto graph)
    {
        var map = _repo.SaveGraph(
            graph.Id == 0 ? null : graph.Id,
            graph.Name,
            graph.Nodes.Select(n => (n.Id, n.X, n.Y)),
            graph.Paths.Select(p => (p.Id, p.StartNodeId, p.EndNodeId, p.TwoWay)),
            graph.Points.Select(pt => (pt.Id, pt.PathId, pt.Type, pt.Name, pt.Offset)),
            graph.Qrs.Select(q => (q.Id, q.PathId, q.Data, q.OffsetStart))
        );
        return map;
    }
}
