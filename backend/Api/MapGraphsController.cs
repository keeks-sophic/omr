using Backend.Dto;
using Backend.Infrastructure.Persistence;
using Backend.Mapping;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Backend.Api;

[ApiController]
public class MapGraphsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MapGraphsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("/maps")]
    public async Task<ActionResult<IEnumerable<object>>> ListMaps(CancellationToken ct)
    {
        var maps = await _db.Maps.AsNoTracking().Select(m => new { id = m.Id, name = m.Name }).ToListAsync(ct);
        return Ok(maps);
    }

    [HttpGet("/maps/{id}/graph")]
    public async Task<ActionResult<object>> GetGraph(int id, CancellationToken ct)
    {
        var map = await _db.Maps.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (map == null) return NotFound();

        var nodeEntities = await _db.Nodes.AsNoTracking().Where(n => n.MapId == id).ToListAsync(ct);
        var pathEntities = await _db.Paths.AsNoTracking().Where(p => p.MapId == id).ToListAsync(ct);
        var pointEntities = await _db.Points.AsNoTracking().Where(p => p.MapId == id).ToListAsync(ct);
        var qrEntities = await _db.Qrs.AsNoTracking().Where(q => q.MapId == id).ToListAsync(ct);
        var nodes = nodeEntities.Select(NodeMapper.ToDto).ToList();
        var paths = pathEntities.Select(PathMapper.ToDto).ToList();
        var points = pointEntities.Select(MapPointMapper.ToDto).ToList();
        var qrs = qrEntities.Select(QrMapper.ToDto).ToList();

        return Ok(new { nodes, paths, points, qrs });
    }

    [HttpPost("/maps/graph")]
    public async Task<ActionResult<object>> SaveGraph([FromBody] SaveMapGraphRequest req, CancellationToken ct)
    {
        var mapDto = req.Maps ?? req.Map;
        if (mapDto == null || string.IsNullOrWhiteSpace(mapDto.Name)) return BadRequest();

        Maps map;
        var creating = mapDto.Id <= 0;
        if (creating)
        {
            map = MapMapper.ToEntity(mapDto);
            map.Id = 0;
            _db.Maps.Add(map);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            map = await _db.Maps.FirstOrDefaultAsync(m => m.Id == mapDto.Id, ct) ?? new Maps { Id = mapDto.Id };
            if (map.Id <= 0) return NotFound();
            map.Name = mapDto.Name;
            await _db.SaveChangesAsync(ct);

            var existingNodes = await _db.Nodes.Where(n => n.MapId == map.Id).ToListAsync(ct);
            if (existingNodes.Count > 0) { _db.Nodes.RemoveRange(existingNodes); await _db.SaveChangesAsync(ct); }
            var existingPaths = await _db.Paths.Where(p => p.MapId == map.Id).ToListAsync(ct);
            if (existingPaths.Count > 0) { _db.Paths.RemoveRange(existingPaths); await _db.SaveChangesAsync(ct); }
            var existingPoints = await _db.Points.Where(p => p.MapId == map.Id).ToListAsync(ct);
            if (existingPoints.Count > 0) { _db.Points.RemoveRange(existingPoints); await _db.SaveChangesAsync(ct); }
            var existingQrs = await _db.Qrs.Where(q => q.MapId == map.Id).ToListAsync(ct);
            if (existingQrs.Count > 0) { _db.Qrs.RemoveRange(existingQrs); await _db.SaveChangesAsync(ct); }
        }

        var nodeIdMap = new Dictionary<int, int>();
        if (req.Nodes != null && req.Nodes.Count > 0)
        {
            foreach (var n in req.Nodes)
            {
                var entity = NodeMapper.ToEntity(n);
                entity.Id = 0;
                entity.MapId = map.Id;
                _db.Nodes.Add(entity);
                await _db.SaveChangesAsync(ct);
                nodeIdMap[n.Id] = entity.Id;
            }
        }

        var savedPaths = new List<Paths>();
        if (req.Paths != null && req.Paths.Count > 0)
        {
            foreach (var p in req.Paths)
            {
                var entity = PathMapper.ToEntity(p);
                entity.Id = 0;
                entity.MapId = map.Id;
                entity.StartNodeId = nodeIdMap.TryGetValue(p.StartNodeId, out var s) ? s : p.StartNodeId;
                entity.EndNodeId = nodeIdMap.TryGetValue(p.EndNodeId, out var e) ? e : p.EndNodeId;
                var startNode = await _db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == entity.StartNodeId, ct);
                var endNode = await _db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == entity.EndNodeId, ct);
                if (startNode != null && endNode != null)
                {
                    var sc = startNode.Location != null ? startNode.Location.Coordinate : new Coordinate(startNode.X, startNode.Y);
                    var ec = endNode.Location != null ? endNode.Location.Coordinate : new Coordinate(endNode.X, endNode.Y);
                    entity.Location = new LineString(new[] { sc, ec }) { SRID = 0 };
                }
                _db.Paths.Add(entity);
                await _db.SaveChangesAsync(ct);
                savedPaths.Add(entity);
            }
        }

        if (req.Points != null && req.Points.Count > 0)
        {
            foreach (var pt in req.Points)
            {
                var entity = MapPointMapper.ToEntity(pt);
                entity.Id = 0;
                entity.MapId = map.Id;
                var idx = pt.PathId;
                if (idx >= 0 && idx < savedPaths.Count)
                {
                    var path = savedPaths[idx];
                    entity.PathId = path.Id;
                    var sc = path.Location != null && path.Location.NumPoints >= 1 ? path.Location.GetCoordinateN(0) : null;
                    var ec = path.Location != null && path.Location.NumPoints >= 2 ? path.Location.GetCoordinateN(path.Location.NumPoints - 1) : null;
                    if (sc != null && ec != null && path.Length > 0)
                    {
                        var t = Math.Max(0, Math.Min(1, entity.Offset / path.Length));
                        var x = sc.X + (ec.X - sc.X) * t;
                        var y = sc.Y + (ec.Y - sc.Y) * t;
                        entity.Location = new NetTopologySuite.Geometries.Point(x, y) { SRID = 0 };
                    }
                }
                _db.Points.Add(entity);
                await _db.SaveChangesAsync(ct);
            }
        }

        if (req.Qrs != null && req.Qrs.Count > 0)
        {
            foreach (var qr in req.Qrs)
            {
                var entity = QrMapper.ToEntity(qr);
                entity.Id = 0;
                entity.MapId = map.Id;
                var idx = qr.PathId;
                if (idx >= 0 && idx < savedPaths.Count)
                {
                    entity.PathId = savedPaths[idx].Id;
                }
                _db.Qrs.Add(entity);
                await _db.SaveChangesAsync(ct);
            }
        }

        return Ok(new { id = map.Id });
    }
}
