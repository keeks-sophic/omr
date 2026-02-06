using BackendV3.Infrastructure.Persistence;
using BackendV3.Modules.Maps.Dto;
using BackendV3.Modules.Maps.Dto.Requests;
using BackendV3.Modules.Maps.Mapping;
using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace BackendV3.Modules.Maps.Service;

public sealed class MapManagementService
{
    private readonly AppDbContext _db;
    private readonly MapHubPublisher _hub;

    public MapManagementService(AppDbContext db, MapHubPublisher hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<MapDto?> CreateMapAsync(CreateMapRequest req, Guid? createdBy, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var name = (req.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name)) return null;

        var nameLower = name.ToLowerInvariant();
        var exists = await _db.Maps.AsNoTracking().AnyAsync(x => x.Name.ToLower() == nameLower, ct);
        if (exists) return null;

        var map = new Map
        {
            MapId = Guid.NewGuid(),
            Name = name,
            CreatedBy = createdBy,
            CreatedAt = now,
            ArchivedAt = null,
            ActivePublishedMapVersionId = null
        };

        var initial = new MapVersion
        {
            MapVersionId = Guid.NewGuid(),
            MapId = map.MapId,
            Version = 1,
            Status = MapVersionStatuses.Draft,
            CreatedBy = createdBy,
            CreatedAt = now
        };

        _db.Maps.Add(map);
        _db.MapVersions.Add(initial);
        await _db.SaveChangesAsync(ct);
        await _hub.MapVersionCreatedAsync(map.MapId, initial.MapVersionId, ct);
        return MapMappers.ToDto(map, now);
    }

    public async Task<List<MapDto>> ListMapsAsync(CancellationToken ct)
    {
        var list = await _db.Maps.AsNoTracking()
            .Where(x => x.ArchivedAt == null)
            .GroupJoin(
                _db.MapVersions.AsNoTracking(),
                m => m.MapId,
                v => v.MapId,
                (m, versions) => new
                {
                    Map = m,
                    UpdatedAt = versions.Max(v => (DateTimeOffset?)(v.PublishedAt ?? v.CreatedAt)) ?? m.CreatedAt
                })
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);

        return list.Select(x => MapMappers.ToDto(x.Map, x.UpdatedAt)).ToList();
    }

    public async Task<MapDto?> GetMapAsync(Guid mapId, CancellationToken ct)
    {
        var row = await _db.Maps.AsNoTracking()
            .Where(x => x.MapId == mapId)
            .GroupJoin(
                _db.MapVersions.AsNoTracking(),
                m => m.MapId,
                v => v.MapId,
                (m, versions) => new
                {
                    Map = m,
                    UpdatedAt = versions.Max(v => (DateTimeOffset?)(v.PublishedAt ?? v.CreatedAt)) ?? m.CreatedAt
                })
            .FirstOrDefaultAsync(ct);

        return row == null ? null : MapMappers.ToDto(row.Map, row.UpdatedAt);
    }

    public async Task<List<MapVersionDto>?> ListVersionsAsync(Guid mapId, CancellationToken ct)
    {
        var mapExists = await _db.Maps.AsNoTracking().AnyAsync(x => x.MapId == mapId, ct);
        if (!mapExists) return null;

        var list = await _db.MapVersions.AsNoTracking()
            .Where(x => x.MapId == mapId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return list.Select(MapMappers.ToDto).ToList();
    }

    public async Task<MapVersionDto?> GetVersionAsync(Guid mapId, Guid mapVersionId, CancellationToken ct)
    {
        var mv = await _db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        return mv == null ? null : MapMappers.ToDto(mv);
    }

    public async Task<MapVersionDto?> GetOrCreateDraftAsync(Guid mapId, Guid? createdBy, CancellationToken ct)
    {
        var map = await _db.Maps.FirstOrDefaultAsync(x => x.MapId == mapId, ct);
        if (map == null) return null;

        var existing = await _db.MapVersions.AsNoTracking()
            .Where(x => x.MapId == mapId && x.Status == MapVersionStatuses.Draft && x.PublishedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (existing != null) return MapMappers.ToDto(existing);

        MapVersion? source = null;
        if (map.ActivePublishedMapVersionId.HasValue)
        {
            source = await _db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.MapId == mapId && x.MapVersionId == map.ActivePublishedMapVersionId.Value, ct);
        }

        if (source == null)
        {
            source = await _db.MapVersions.AsNoTracking()
                .Where(x => x.MapId == mapId && x.PublishedAt != null)
                .OrderByDescending(x => x.PublishedAt)
                .FirstOrDefaultAsync(ct);
        }

        if (source == null)
        {
            source = await _db.MapVersions.AsNoTracking()
                .Where(x => x.MapId == mapId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        if (source == null) return null;

        return await CloneVersionAsync(mapId, source.MapVersionId, new CloneMapRequest(), createdBy, ct);
    }

    public async Task<MapVersionDto?> CloneVersionAsync(Guid mapId, Guid mapVersionId, CloneMapRequest req, Guid? createdBy, CancellationToken ct)
    {
        var mapExists = await _db.Maps.AsNoTracking().AnyAsync(x => x.MapId == mapId, ct);
        if (!mapExists) return null;

        var src = await _db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (src == null) return null;

        var now = DateTimeOffset.UtcNow;
        var nextVersion = (await _db.MapVersions.AsNoTracking().Where(x => x.MapId == mapId).MaxAsync(x => (int?)x.Version, ct) ?? 0) + 1;
        var label = (req.Label ?? req.Name ?? string.Empty).Trim();
        var mv = new MapVersion
        {
            MapVersionId = Guid.NewGuid(),
            MapId = mapId,
            Version = nextVersion,
            Status = MapVersionStatuses.Draft,
            CreatedBy = createdBy,
            CreatedAt = now,
            DerivedFromMapVersionId = src.MapVersionId,
            Label = string.IsNullOrWhiteSpace(label) ? null : label
        };
        _db.MapVersions.Add(mv);

        var nodes = await _db.MapNodes.AsNoTracking().Where(x => x.MapVersionId == src.MapVersionId).ToListAsync(ct);
        var paths = await _db.MapPaths.AsNoTracking().Where(x => x.MapVersionId == src.MapVersionId).ToListAsync(ct);
        var points = await _db.MapPoints.AsNoTracking().Where(x => x.MapVersionId == src.MapVersionId).ToListAsync(ct);
        var qrs = await _db.QrAnchors.AsNoTracking().Where(x => x.MapVersionId == src.MapVersionId).ToListAsync(ct);

        foreach (var n in nodes)
        {
            _db.MapNodes.Add(new MapNode
            {
                NodeId = n.NodeId,
                MapVersionId = mv.MapVersionId,
                Label = n.Label,
                Location = n.Location,
                IsActive = n.IsActive,
                IsMaintenance = n.IsMaintenance,
                JunctionSpeedLimit = n.JunctionSpeedLimit,
                MetadataJson = n.MetadataJson
            });
        }

        foreach (var p in paths)
        {
            _db.MapPaths.Add(new MapPath
            {
                PathId = p.PathId,
                MapVersionId = mv.MapVersionId,
                FromNodeId = p.FromNodeId,
                ToNodeId = p.ToNodeId,
                Direction = p.Direction,
                Location = p.Location,
                LengthMeters = p.LengthMeters,
                IsActive = p.IsActive,
                IsMaintenance = p.IsMaintenance,
                SpeedLimit = p.SpeedLimit,
                IsRestPath = p.IsRestPath,
                RestCapacity = p.RestCapacity,
                RestDwellPolicy = p.RestDwellPolicy,
                MetadataJson = p.MetadataJson
            });
        }

        foreach (var pt in points)
        {
            _db.MapPoints.Add(new MapPoint
            {
                PointId = pt.PointId,
                MapVersionId = mv.MapVersionId,
                Type = pt.Type,
                Label = pt.Label,
                Location = pt.Location,
                AttachedNodeId = pt.AttachedNodeId,
                MetadataJson = pt.MetadataJson
            });
        }

        foreach (var q in qrs)
        {
            _db.QrAnchors.Add(new QrAnchor
            {
                QrId = q.QrId,
                MapVersionId = mv.MapVersionId,
                PathId = q.PathId,
                QrCode = q.QrCode,
                DistanceAlongPath = q.DistanceAlongPath,
                Location = q.Location,
                MetadataJson = q.MetadataJson
            });
        }

        await _db.SaveChangesAsync(ct);
        await EnforceDraftRetentionAsync(mapId, ct);
        await _hub.MapVersionCreatedAsync(mapId, mv.MapVersionId, ct);
        return MapMappers.ToDto(mv);
    }

    public async Task<bool> PublishVersionAsync(Guid mapId, Guid mapVersionId, PublishMapRequest req, Guid? publishedBy, CancellationToken ct)
    {
        var map = await _db.Maps.FirstOrDefaultAsync(x => x.MapId == mapId, ct);
        if (map == null) return false;

        var mv = await _db.MapVersions.FirstOrDefaultAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (mv == null) return false;
        if (!IsEditableDraft(mv)) return false;

        mv.Status = MapVersionStatuses.Published;
        mv.PublishedAt = DateTimeOffset.UtcNow;
        mv.PublishedBy = publishedBy;
        mv.ChangeSummary = req.ChangeSummary;

        map.ActivePublishedMapVersionId = mapVersionId;

        await _db.SaveChangesAsync(ct);
        await _hub.MapVersionPublishedAsync(mapId, mapVersionId, ct);
        return true;
    }

    public async Task<bool> SetActivePublishedVersionAsync(Guid mapId, Guid mapVersionId, CancellationToken ct)
    {
        var map = await _db.Maps.FirstOrDefaultAsync(x => x.MapId == mapId, ct);
        if (map == null) return false;

        var mv = await _db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (mv == null) return false;
        if (!string.Equals(mv.Status, MapVersionStatuses.Published, StringComparison.OrdinalIgnoreCase)) return false;

        map.ActivePublishedMapVersionId = mapVersionId;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<NodeDto[]?> ListNodesAsync(Guid mapId, Guid mapVersionId, CancellationToken ct)
    {
        var exists = await _db.MapVersions.AsNoTracking().AnyAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (!exists) return null;
        var nodes = await _db.MapNodes.AsNoTracking().Where(x => x.MapVersionId == mapVersionId).ToListAsync(ct);
        return nodes.Select(MapMappers.ToDto).ToArray();
    }

    public async Task<NodeDto?> CreateNodeAsync(Guid mapId, Guid mapVersionId, CreateNodeRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;
        var id = TryParseOrNew(req.NodeId, out var parsed) ? parsed : Guid.NewGuid();
        if (await _db.MapNodes.AsNoTracking().AnyAsync(x => x.MapVersionId == mapVersionId && x.NodeId == id, ct)) return null;

        var now = DateTimeOffset.UtcNow;
        var node = new MapNode
        {
            NodeId = id,
            MapVersionId = mapVersionId,
            Label = (req.Label ?? string.Empty).Trim(),
            Location = MapGeometry.MakePoint(req.Geom.X, req.Geom.Y),
            IsActive = true,
            IsMaintenance = false,
            JunctionSpeedLimit = req.JunctionSpeedLimit
        };
        _db.MapNodes.Add(node);
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "node", node.NodeId, ct);
        return MapMappers.ToDto(node);
    }

    public async Task<NodeDto?> UpdateNodeAsync(Guid mapId, Guid mapVersionId, Guid nodeId, UpdateNodeRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;
        var node = await _db.MapNodes.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.NodeId == nodeId, ct);
        if (node == null) return null;

        var now = DateTimeOffset.UtcNow;
        node.Label = (req.Label ?? string.Empty).Trim();
        node.Location = MapGeometry.MakePoint(req.Geom.X, req.Geom.Y);
        node.JunctionSpeedLimit = req.JunctionSpeedLimit;

        var paths = await _db.MapPaths.Where(p => p.MapVersionId == mapVersionId && (p.FromNodeId == nodeId || p.ToNodeId == nodeId)).ToListAsync(ct);
        if (paths.Count > 0)
        {
            var nodes = await _db.MapNodes.AsNoTracking().Where(n => n.MapVersionId == mapVersionId).ToDictionaryAsync(n => n.NodeId, ct);
            foreach (var p in paths)
            {
                if (!nodes.TryGetValue(p.FromNodeId, out var from) || !nodes.TryGetValue(p.ToNodeId, out var to)) continue;
                var line = MapGeometry.MakeLine(from.Location, to.Location);
                p.Location = line;
                p.LengthMeters = MapGeometry.Length(line);
            }

            var pathIds = paths.Select(x => x.PathId).ToArray();
            var qrs = await _db.QrAnchors.Where(q => q.MapVersionId == mapVersionId && pathIds.Contains(q.PathId)).ToListAsync(ct);
            foreach (var q in qrs)
            {
                var path = paths.FirstOrDefault(x => x.PathId == q.PathId);
                if (path == null) continue;
                q.Location = MapGeometry.PointAlong(path.Location, q.DistanceAlongPath);
            }
        }

        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "node", node.NodeId, ct);
        return MapMappers.ToDto(node);
    }

    public async Task<bool> SetNodeMaintenanceAsync(Guid mapId, Guid mapVersionId, Guid nodeId, bool isMaintenance, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return false;
        var node = await _db.MapNodes.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.NodeId == nodeId, ct);
        if (node == null) return false;
        var now = DateTimeOffset.UtcNow;
        node.IsMaintenance = isMaintenance;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "node", node.NodeId, ct);
        return true;
    }

    public async Task<PathDto[]?> ListPathsAsync(Guid mapId, Guid mapVersionId, CancellationToken ct)
    {
        var exists = await _db.MapVersions.AsNoTracking().AnyAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (!exists) return null;
        var paths = await _db.MapPaths.AsNoTracking().Where(x => x.MapVersionId == mapVersionId).ToListAsync(ct);
        return paths.Select(MapMappers.ToDto).ToArray();
    }

    public async Task<PathDto?> CreatePathAsync(Guid mapId, Guid mapVersionId, CreatePathRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;

        if (!TryParseGuid(req.FromNodeId, out var fromId) || !TryParseGuid(req.ToNodeId, out var toId)) return null;
        var from = await _db.MapNodes.AsNoTracking().FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.NodeId == fromId, ct);
        var to = await _db.MapNodes.AsNoTracking().FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.NodeId == toId, ct);
        if (from == null || to == null) return null;

        var id = TryParseOrNew(req.PathId, out var parsed) ? parsed : Guid.NewGuid();
        if (await _db.MapPaths.AsNoTracking().AnyAsync(x => x.MapVersionId == mapVersionId && x.PathId == id, ct)) return null;

        var line = MapGeometry.MakeLine(from.Location, to.Location);
        var path = new MapPath
        {
            PathId = id,
            MapVersionId = mapVersionId,
            FromNodeId = fromId,
            ToNodeId = toId,
            Direction = NormalizeDirection(req.Direction),
            Location = line,
            LengthMeters = MapGeometry.Length(line),
            IsActive = true,
            IsMaintenance = false,
            SpeedLimit = req.SpeedLimit,
            IsRestPath = false
        };
        _db.MapPaths.Add(path);
        var now = DateTimeOffset.UtcNow;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "path", path.PathId, ct);
        return MapMappers.ToDto(path);
    }

    public async Task<PathDto?> UpdatePathAsync(Guid mapId, Guid mapVersionId, Guid pathId, UpdatePathRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;
        var path = await _db.MapPaths.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.PathId == pathId, ct);
        if (path == null) return null;
        path.Direction = NormalizeDirection(req.Direction);
        path.SpeedLimit = req.SpeedLimit;
        var now = DateTimeOffset.UtcNow;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "path", path.PathId, ct);
        return MapMappers.ToDto(path);
    }

    public async Task<bool> SetPathMaintenanceAsync(Guid mapId, Guid mapVersionId, Guid pathId, bool isMaintenance, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return false;
        var path = await _db.MapPaths.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.PathId == pathId, ct);
        if (path == null) return false;
        path.IsMaintenance = isMaintenance;
        var now = DateTimeOffset.UtcNow;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "path", path.PathId, ct);
        return true;
    }

    public async Task<PathDto?> SetPathRestAsync(Guid mapId, Guid mapVersionId, Guid pathId, SetRestOptionsRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;
        var path = await _db.MapPaths.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.PathId == pathId, ct);
        if (path == null) return null;
        path.IsRestPath = req.IsRestPath;
        path.RestCapacity = req.IsRestPath ? req.RestCapacity : null;
        path.RestDwellPolicy = req.IsRestPath ? req.RestDwellPolicy : null;
        var now = DateTimeOffset.UtcNow;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "path", path.PathId, ct);
        return MapMappers.ToDto(path);
    }

    public async Task<MapPointDto[]?> ListPointsAsync(Guid mapId, Guid mapVersionId, CancellationToken ct)
    {
        var exists = await _db.MapVersions.AsNoTracking().AnyAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (!exists) return null;
        var points = await _db.MapPoints.AsNoTracking().Where(x => x.MapVersionId == mapVersionId).ToListAsync(ct);
        return points.Select(MapMappers.ToDto).ToArray();
    }

    public async Task<MapPointDto?> CreatePointAsync(Guid mapId, Guid mapVersionId, CreatePointRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;
        var id = TryParseOrNew(req.PointId, out var parsed) ? parsed : Guid.NewGuid();
        if (await _db.MapPoints.AsNoTracking().AnyAsync(x => x.MapVersionId == mapVersionId && x.PointId == id, ct)) return null;

        Guid? attached = null;
        if (!string.IsNullOrWhiteSpace(req.AttachedNodeId))
        {
            if (!TryParseGuid(req.AttachedNodeId, out var nodeId)) return null;
            attached = nodeId;
        }

        var pt = new MapPoint
        {
            PointId = id,
            MapVersionId = mapVersionId,
            Type = (req.Type ?? string.Empty).Trim(),
            Label = (req.Label ?? string.Empty).Trim(),
            Location = MapGeometry.MakePoint(req.Geom.X, req.Geom.Y),
            AttachedNodeId = attached
        };
        _db.MapPoints.Add(pt);
        var now = DateTimeOffset.UtcNow;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "point", pt.PointId, ct);
        return MapMappers.ToDto(pt);
    }

    public async Task<MapPointDto?> UpdatePointAsync(Guid mapId, Guid mapVersionId, Guid pointId, UpdatePointRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;
        var pt = await _db.MapPoints.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.PointId == pointId, ct);
        if (pt == null) return null;

        Guid? attached = null;
        if (!string.IsNullOrWhiteSpace(req.AttachedNodeId))
        {
            if (!TryParseGuid(req.AttachedNodeId, out var nodeId)) return null;
            attached = nodeId;
        }

        pt.Type = (req.Type ?? string.Empty).Trim();
        pt.Label = (req.Label ?? string.Empty).Trim();
        pt.Location = MapGeometry.MakePoint(req.Geom.X, req.Geom.Y);
        pt.AttachedNodeId = attached;
        var now = DateTimeOffset.UtcNow;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "point", pt.PointId, ct);
        return MapMappers.ToDto(pt);
    }

    public async Task<QrDto[]?> ListQrsAsync(Guid mapId, Guid mapVersionId, CancellationToken ct)
    {
        var exists = await _db.MapVersions.AsNoTracking().AnyAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (!exists) return null;
        var qrs = await _db.QrAnchors.AsNoTracking().Where(x => x.MapVersionId == mapVersionId).ToListAsync(ct);
        return qrs.Select(MapMappers.ToDto).ToArray();
    }

    public async Task<QrDto?> CreateQrAsync(Guid mapId, Guid mapVersionId, CreateQrRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;
        if (!TryParseGuid(req.PathId, out var pathId)) return null;
        var path = await _db.MapPaths.AsNoTracking().FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.PathId == pathId, ct);
        if (path == null) return null;

        var id = TryParseOrNew(req.QrId, out var parsed) ? parsed : Guid.NewGuid();
        if (await _db.QrAnchors.AsNoTracking().AnyAsync(x => x.MapVersionId == mapVersionId && x.QrId == id, ct)) return null;

        var qr = new QrAnchor
        {
            QrId = id,
            MapVersionId = mapVersionId,
            PathId = pathId,
            QrCode = (req.QrCode ?? string.Empty).Trim(),
            DistanceAlongPath = req.DistanceAlongPath,
            Location = MapGeometry.PointAlong(path.Location, req.DistanceAlongPath)
        };
        _db.QrAnchors.Add(qr);
        var now = DateTimeOffset.UtcNow;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "qr", qr.QrId, ct);
        return MapMappers.ToDto(qr);
    }

    public async Task<QrDto?> UpdateQrAsync(Guid mapId, Guid mapVersionId, Guid qrId, UpdateQrRequest req, CancellationToken ct)
    {
        var mv = await GetEditableVersionAsync(mapId, mapVersionId, ct);
        if (mv == null) return null;
        var qr = await _db.QrAnchors.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.QrId == qrId, ct);
        if (qr == null) return null;
        if (!TryParseGuid(req.PathId, out var pathId)) return null;
        var path = await _db.MapPaths.AsNoTracking().FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId && x.PathId == pathId, ct);
        if (path == null) return null;

        qr.PathId = pathId;
        qr.QrCode = (req.QrCode ?? string.Empty).Trim();
        qr.DistanceAlongPath = req.DistanceAlongPath;
        qr.Location = MapGeometry.PointAlong(path.Location, req.DistanceAlongPath);
        var now = DateTimeOffset.UtcNow;
        await TouchMapUpdatedAtAsync(mv.MapId, now, ct);
        await _db.SaveChangesAsync(ct);
        await _hub.MapEntityUpdatedAsync(mv.MapId, mapVersionId, "qr", qr.QrId, ct);
        return MapMappers.ToDto(qr);
    }

    public static Guid? GetActorUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        var sub = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var g) ? g : null;
    }

    private static bool TryParseGuid(string input, out Guid value)
    {
        return Guid.TryParse(input, out value);
    }

    private static bool TryParseOrNew(string? input, out Guid value)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            value = Guid.NewGuid();
            return false;
        }

        if (Guid.TryParse(input, out var g))
        {
            value = g;
            return true;
        }

        value = Guid.Empty;
        return false;
    }

    private static string NormalizeDirection(string? direction)
    {
        if (string.Equals(direction, "ONE_WAY", StringComparison.OrdinalIgnoreCase)) return "ONE_WAY";
        return "TWO_WAY";
    }

    private async Task<MapVersion?> GetEditableVersionAsync(Guid mapId, Guid mapVersionId, CancellationToken ct)
    {
        var mv = await _db.MapVersions.FirstOrDefaultAsync(x => x.MapId == mapId && x.MapVersionId == mapVersionId, ct);
        if (mv == null) return null;
        return IsEditableDraft(mv) ? mv : null;
    }

    private static bool IsEditableDraft(MapVersion mv)
    {
        if (mv.PublishedAt != null) return false;
        return string.Equals(mv.Status, MapVersionStatuses.Draft, StringComparison.OrdinalIgnoreCase);
    }

    private async Task TouchMapUpdatedAtAsync(Guid mapId, DateTimeOffset now, CancellationToken ct)
    {
        await Task.CompletedTask;
    }

    private async Task EnforceDraftRetentionAsync(Guid mapId, CancellationToken ct)
    {
        const int keep = 5;
        var drafts = await _db.MapVersions.AsNoTracking()
            .Where(x => x.MapId == mapId && x.Status == MapVersionStatuses.Draft && x.PublishedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.MapVersionId)
            .ToListAsync(ct);

        if (drafts.Count <= keep) return;

        var toDelete = drafts.Skip(keep).ToArray();
        if (toDelete.Length == 0) return;

        var nodes = await _db.MapNodes.Where(x => toDelete.Contains(x.MapVersionId)).ToListAsync(ct);
        var paths = await _db.MapPaths.Where(x => toDelete.Contains(x.MapVersionId)).ToListAsync(ct);
        var points = await _db.MapPoints.Where(x => toDelete.Contains(x.MapVersionId)).ToListAsync(ct);
        var qrs = await _db.QrAnchors.Where(x => toDelete.Contains(x.MapVersionId)).ToListAsync(ct);
        var versions = await _db.MapVersions.Where(x => toDelete.Contains(x.MapVersionId)).ToListAsync(ct);

        _db.MapNodes.RemoveRange(nodes);
        _db.MapPaths.RemoveRange(paths);
        _db.MapPoints.RemoveRange(points);
        _db.QrAnchors.RemoveRange(qrs);
        _db.MapVersions.RemoveRange(versions);
        await _db.SaveChangesAsync(ct);
    }
}
