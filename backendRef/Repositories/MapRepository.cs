using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using PathModel = backend.Models.Path;

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
            .Include(m => m.MapPoints)
            .Include(m => m.Qrs)
            .AsNoTracking()
            .FirstOrDefault(m => m.Id == id);
    }

    public Map SaveGraph(
        int? id,
        string name,
        IEnumerable<(int id, double x, double y)> nodes,
        IEnumerable<(int id, int startId, int endId, bool twoWay)> paths,
        IEnumerable<(int id, int? pathId, string type, string name, double offset)> points,
        IEnumerable<(int id, int? pathId, string data, double offsetStart)> qrs
    )
    {
        Map? map;
        if (id.HasValue)
        {
            map = _db.Maps
                .Include(m => m.Nodes)
                .Include(m => m.Paths)
                .Include(m => m.MapPoints)
                .Include(m => m.Qrs)
                .FirstOrDefault(m => m.Id == id.Value);
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
                map.Paths.Add(new PathModel
                {
                    Id = p.id,
                    MapId = map.Id,
                    StartNodeId = p.startId,
                    EndNodeId = p.endId,
                    TwoWay = p.twoWay
                });
            }
        }

        var existingPoints = map.MapPoints.ToDictionary(pp => pp.Id);
        foreach (var pt in points)
        {
            if (existingPoints.TryGetValue(pt.id, out var ept))
            {
                ept.PathId = pt.pathId;
                ept.Type = pt.type;
                ept.Name = pt.name;
                ept.Offset = pt.offset;
            }
            else
            {
                map.MapPoints.Add(new MapPoint
                {
                    Id = pt.id,
                    MapId = map.Id,
                    PathId = pt.pathId,
                    Type = pt.type,
                    Name = pt.name,
                    Offset = pt.offset
                });
            }
        }

        var existingQrs = map.Qrs.ToDictionary(q => q.Id);
        foreach (var q in qrs)
        {
            if (existingQrs.TryGetValue(q.id, out var eqr))
            {
                eqr.PathId = q.pathId;
                eqr.Data = q.data;
                eqr.OffsetStart = q.offsetStart;
                eqr.Location = null;
            }
            else
            {
                map.Qrs.Add(new Qr
                {
                    Id = q.id,
                    MapId = map.Id,
                    PathId = q.pathId,
                    Data = q.data,
                    OffsetStart = q.offsetStart,
                    Location = null
                });
            }
        }

        _db.SaveChanges();
        return map;
    }
}
