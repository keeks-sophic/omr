using Backend.Dto;
using Backend.Infrastructure.Persistence;
using Backend.Mapping;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Backend.Data;

public class MapRepository
{
    private readonly AppDbContext _db;
    public MapRepository(AppDbContext db) { _db = db; }

    public async Task<List<MapDto>> GetAllMapsAsync(CancellationToken ct)
    {
        var entities = await _db.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Paths)
            .Include(m => m.Points)
            .Include(m => m.Qrs)
            .AsNoTracking()
            .ToListAsync(ct);
        return entities.Select(MapMapper.ToDto).ToList();
    }

    public async Task<object?> GetGraphAsync(int id, CancellationToken ct)
    {
        var map = await _db.Maps.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (map == null) return null;
        var nodeEntities = await _db.Nodes.AsNoTracking().Where(n => n.MapId == id).ToListAsync(ct);
        var pathEntities = await _db.Paths.AsNoTracking().Where(p => p.MapId == id).ToListAsync(ct);
        var pointEntities = await _db.Points.AsNoTracking().Where(p => p.MapId == id).ToListAsync(ct);
        var qrEntities = await _db.Qrs.AsNoTracking().Where(q => q.MapId == id).ToListAsync(ct);
        var nodes = nodeEntities.Select(NodeMapper.ToDto).ToList();
        var paths = pathEntities.Select(PathMapper.ToDto).ToList();
        var points = pointEntities.Select(MapPointMapper.ToDto).ToList();
        var qrs = qrEntities.Select(QrMapper.ToDto).ToList();
        return new { nodes, paths, points, qrs };
    }

    public async Task<int> SaveGraphAsync(SaveMapGraphRequest req, CancellationToken ct)
    {
        var mapDto = req.Maps ?? req.Map;
        if (mapDto == null || string.IsNullOrWhiteSpace(mapDto.Name)) throw new InvalidOperationException();

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
            if (map.Id <= 0) throw new KeyNotFoundException();
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

        return map.Id;
    }

    public async Task<(int[] nodeIds, int[] pathIds, double totalLength)?> TryComputeRouteWithPgRoutingAsync(int mapId, int startNodeId, int destNodeId, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        try
        {
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT EXISTS(SELECT 1 FROM pg_extension WHERE extname='pgrouting')";
            var existsObj = await checkCmd.ExecuteScalarAsync(ct);
            var exists = existsObj is bool b ? b : (existsObj is int i ? i != 0 : false);
            if (!exists) return null;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                WITH edges AS (
                    SELECT id, startnodeid AS source, endnodeid AS target, length AS cost
                    FROM paths
                    WHERE mapid = @map AND lower(status) = 'active'
                )
                SELECT seq, path_seq, node, edge, cost, agg_cost
                FROM pgr_dijkstra(
                    'SELECT id, source, target, cost FROM edges',
                    @start, @dest, false
                )
                ORDER BY path_seq";
            var pMap = cmd.CreateParameter(); pMap.ParameterName = "@map"; pMap.Value = mapId; cmd.Parameters.Add(pMap);
            var pStart = cmd.CreateParameter(); pStart.ParameterName = "@start"; pStart.Value = startNodeId; cmd.Parameters.Add(pStart);
            var pDest = cmd.CreateParameter(); pDest.ParameterName = "@dest"; pDest.Value = destNodeId; cmd.Parameters.Add(pDest);
            var nodes = new List<int>();
            var edges = new List<int>();
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var node = reader.GetFieldValue<int>(reader.GetOrdinal("node"));
                var edge = reader.GetFieldValue<int>(reader.GetOrdinal("edge"));
                if (nodes.Count == 0 || nodes[^1] != node) nodes.Add(node);
                if (edge != -1) edges.Add(edge);
            }
            await reader.CloseAsync();
            if (nodes.Count == 0) return null;
            var pathIds = edges.Distinct().ToArray();
            double totalLength = 0.0;
            if (pathIds.Length > 0)
            {
                totalLength = await _db.Paths.AsNoTracking().Where(p => pathIds.Contains(p.Id)).Select(p => p.Location != null ? p.Location.Length : p.Length).SumAsync(ct);
            }
            return (nodes.ToArray(), pathIds, totalLength);
        }
        catch
        {
            return null;
        }
        finally
        {
            await conn.CloseAsync();
        }
    }
}
