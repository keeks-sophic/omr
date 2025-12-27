using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class MapRepository : IMapRepository
{
    private readonly ApplicationDbContext _db;
    public MapRepository(ApplicationDbContext db) { _db = db; }

    public IEnumerable<Map> GetAll()
    {
        return _db.Maps.AsNoTracking().OrderBy(m => m.Id).ToArray();
    }

    public Map? FindByIdWithGraph(int id)
    {
        return _db.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Paths)
            .AsNoTracking()
            .FirstOrDefault(m => m.Id == id);
    }

    public Map SaveGraph(int? id, string name, IEnumerable<(int id, double x, double y)> nodes, IEnumerable<(int id, int startId, int endId, bool twoWay)> paths)
    {
        Map? map;
        if (id.HasValue)
        {
            map = _db.Maps.Include(m => m.Nodes).Include(m => m.Paths).FirstOrDefault(m => m.Id == id.Value);
        }
        else
        {
            map = null;
        }
        if (map is null)
        {
            map = new Map { Name = name };
            _db.Maps.Add(map);
            _db.SaveChanges();
        }
        else
        {
            map.Name = name;
        }

        var existingNodes = map.Nodes.ToDictionary(n => n.Id);
        foreach (var n in nodes)
        {
            if (existingNodes.TryGetValue(n.id, out var en))
            {
                en.X = n.x;
                en.Y = n.y;
                en.Location = new NetTopologySuite.Geometries.Point(n.x, n.y) { SRID = 0 };
            }
            else
            {
                map.Nodes.Add(new Node
                {
                    Id = n.id,
                    MapId = map.Id,
                    X = n.x,
                    Y = n.y,
                    Location = new NetTopologySuite.Geometries.Point(n.x, n.y) { SRID = 0 }
                });
            }
        }

        var existingPaths = map.Paths.ToDictionary(p => p.Id);
        foreach (var p in paths)
        {
            if (existingPaths.TryGetValue(p.id, out var ep))
            {
                ep.StartNodeId = p.startId;
                ep.EndNodeId = p.endId;
                ep.TwoWay = p.twoWay;
            }
            else
            {
                map.Paths.Add(new Path
                {
                    Id = p.id,
                    MapId = map.Id,
                    StartNodeId = p.startId,
                    EndNodeId = p.endId,
                    TwoWay = p.twoWay
                });
            }
        }

        _db.SaveChanges();
        return map;
    }
}
