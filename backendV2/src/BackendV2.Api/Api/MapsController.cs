using System;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Maps;
using BackendV2.Api.Infrastructure.Persistence;
using BackendV2.Api.Model.Map;
using BackendV2.Api.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/maps")]
public class MapsController : ControllerBase
{
[Authorize]
[HttpGet]
public async Task<IActionResult> ListVersions([FromServices] AppDbContext db)
{
    var versions = await db.MapVersions.AsNoTracking().Select(m => new { mapVersionId = m.MapVersionId, name = m.Name, version = m.Version, isActive = m.IsActive, publishedAt = m.PublishedAt }).ToListAsync();
    return Ok(versions);
}

[Authorize]
[HttpGet("{mapVersionId}")]
public async Task<IActionResult> GetVersion(Guid mapVersionId, [FromServices] AppDbContext db)
{
    var m = await db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId);
    if (m == null) return NotFound();
    return Ok(new { mapVersionId = m.MapVersionId, name = m.Name, version = m.Version, isActive = m.IsActive, publishedAt = m.PublishedAt, changeSummary = m.ChangeSummary });
}
[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost]
public async Task<IActionResult> CreateVersion([FromBody] MapVersionCreateRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var mv = new MapVersion { MapVersionId = Guid.NewGuid(), Name = req.Name, Version = 1, IsActive = false, CreatedBy = null, CreatedAt = DateTimeOffset.UtcNow, ChangeSummary = req.ChangeSummary };
        await db.MapVersions.AddAsync(mv);
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapVersionCreated, new object[] { new { mapVersionId = mv.MapVersionId.ToString(), name = mv.Name, version = mv.Version } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g0) ? g0 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.version.create", TargetType = "map", TargetId = mv.MapVersionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(mv);
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("{mapVersionId}/clone")]
public async Task<IActionResult> CloneVersion(Guid mapVersionId, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var src = await db.MapVersions.AsNoTracking().FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId);
        if (src == null) return NotFound();
        var mv = new MapVersion { MapVersionId = Guid.NewGuid(), Name = src.Name, Version = src.Version + 1, IsActive = false, CreatedAt = DateTimeOffset.UtcNow, ChangeSummary = "cloned" };
        await db.MapVersions.AddAsync(mv);
        var nodes = await db.Nodes.AsNoTracking().Where(n => n.MapVersionId == mapVersionId).ToListAsync();
        var paths = await db.Paths.AsNoTracking().Where(p => p.MapVersionId == mapVersionId).ToListAsync();
        var points = await db.Points.AsNoTracking().Where(p => p.MapVersionId == mapVersionId).ToListAsync();
        var qrs = await db.QrAnchors.AsNoTracking().Where(q => q.MapVersionId == mapVersionId).ToListAsync();
        foreach (var n in nodes) await db.Nodes.AddAsync(new MapNode { NodeId = Guid.NewGuid(), MapVersionId = mv.MapVersionId, Name = n.Name, Location = n.Location, IsActive = n.IsActive, IsMaintenance = n.IsMaintenance, MetadataJson = n.MetadataJson });
        foreach (var p in paths) await db.Paths.AddAsync(new MapPath { PathId = Guid.NewGuid(), MapVersionId = mv.MapVersionId, FromNodeId = p.FromNodeId, ToNodeId = p.ToNodeId, Location = p.Location, TwoWay = p.TwoWay, IsActive = p.IsActive, IsMaintenance = p.IsMaintenance, SpeedLimit = p.SpeedLimit, IsRestPath = p.IsRestPath, RestCapacity = p.RestCapacity, RestDwellPolicyJson = p.RestDwellPolicyJson, MinFollowingDistanceMeters = p.MinFollowingDistanceMeters, MetadataJson = p.MetadataJson });
        foreach (var p in points) await db.Points.AddAsync(new MapPoint { PointId = Guid.NewGuid(), MapVersionId = mv.MapVersionId, Name = p.Name, Type = p.Type, Location = p.Location, AttachedNodeId = p.AttachedNodeId, MetadataJson = p.MetadataJson });
        foreach (var q in qrs) await db.QrAnchors.AddAsync(new QrAnchor { QrId = Guid.NewGuid(), MapVersionId = mv.MapVersionId, QrCode = q.QrCode, Location = q.Location, PathId = q.PathId, DistanceAlongPath = q.DistanceAlongPath, MetadataJson = q.MetadataJson });
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapVersionCreated, new object[] { new { mapVersionId = mv.MapVersionId.ToString(), name = mv.Name, version = mv.Version } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g1) ? g1 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.version.clone", TargetType = "map", TargetId = mv.MapVersionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(mv);
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("{mapVersionId}/publish")]
public async Task<IActionResult> Publish(Guid mapVersionId, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
    var mv = await db.MapVersions.FirstOrDefaultAsync(x => x.MapVersionId == mapVersionId);
    if (mv == null) return NotFound();
    mv.IsActive = true;
    mv.PublishedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
    await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapVersionPublished, new object[] { new { mapVersionId = mv.MapVersionId.ToString() } }, System.Threading.CancellationToken.None);
    var actor = User.FindFirst("sub")?.Value;
    System.Guid? actorId = System.Guid.TryParse(actor, out var g) ? g : null;
    await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.publish", TargetType = "map", TargetId = mapVersionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
    await db.SaveChangesAsync();
    return Ok(new { ok = true });
}

[Authorize]
[HttpGet("{mapVersionId}/nodes")]
public async Task<IActionResult> ListNodes(Guid mapVersionId, [FromServices] AppDbContext db)
{
    var nodes = await db.Nodes.AsNoTracking().Where(n => n.MapVersionId == mapVersionId).Select(n => new { nodeId = n.NodeId, name = n.Name, x = n.Location.X, y = n.Location.Y, isActive = n.IsActive, isMaintenance = n.IsMaintenance }).ToListAsync();
    return Ok(nodes);
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("{mapVersionId}/nodes")]
public async Task<IActionResult> CreateNode(Guid mapVersionId, [FromBody] NodeRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
    var n = new MapNode { NodeId = Guid.NewGuid(), MapVersionId = mapVersionId, Name = req.Name, Location = new Point(req.X, req.Y) { SRID = 0 }, IsActive = req.IsActive };
    await db.Nodes.AddAsync(n);
    await db.SaveChangesAsync();
    await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "node", id = n.NodeId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
    var actor = User.FindFirst("sub")?.Value;
    System.Guid? actorId = System.Guid.TryParse(actor, out var g) ? g : null;
    await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.node.create", TargetType = "map", TargetId = n.NodeId.ToString(), Outcome = "OK", DetailsJson = "{}" });
    await db.SaveChangesAsync();
    return Ok(n);
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPut("{mapVersionId}/nodes/{nodeId}")]
public async Task<IActionResult> UpdateNode(Guid mapVersionId, Guid nodeId, [FromBody] NodeRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
    var n = await db.Nodes.FirstOrDefaultAsync(x => x.NodeId == nodeId && x.MapVersionId == mapVersionId);
    if (n == null) return NotFound();
    n.Name = req.Name;
    n.Location = new Point(req.X, req.Y) { SRID = 0 };
    n.IsActive = req.IsActive;
    await db.SaveChangesAsync();
    await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "node", id = n.NodeId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
    var actor = User.FindFirst("sub")?.Value;
    System.Guid? actorId = System.Guid.TryParse(actor, out var g) ? g : null;
    await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.node.update", TargetType = "map", TargetId = n.NodeId.ToString(), Outcome = "OK", DetailsJson = "{}" });
    await db.SaveChangesAsync();
    return Ok(n);
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPut("{mapVersionId}/nodes/{nodeId}/maintenance")]
public async Task<IActionResult> ToggleNodeMaintenance(Guid mapVersionId, Guid nodeId, [FromBody] bool isMaintenance, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
    var n = await db.Nodes.FirstOrDefaultAsync(x => x.NodeId == nodeId && x.MapVersionId == mapVersionId);
    if (n == null) return NotFound();
    n.IsMaintenance = isMaintenance;
    await db.SaveChangesAsync();
    await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "node", id = n.NodeId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
    var actor = User.FindFirst("sub")?.Value;
    System.Guid? actorId = System.Guid.TryParse(actor, out var g) ? g : null;
    await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.node.maintenance", TargetType = "map", TargetId = n.NodeId.ToString(), Outcome = "OK", DetailsJson = "{}" });
    await db.SaveChangesAsync();
    return Ok(new { ok = true });
}

[Authorize]
[HttpGet("{mapVersionId}/paths")]
public async Task<IActionResult> ListPaths(Guid mapVersionId, [FromServices] AppDbContext db)
{
    var paths = await db.Paths.AsNoTracking().Where(p => p.MapVersionId == mapVersionId).Select(p => new { pathId = p.PathId, fromNodeId = p.FromNodeId, toNodeId = p.ToNodeId, twoWay = p.TwoWay, isActive = p.IsActive, isMaintenance = p.IsMaintenance, speedLimit = p.SpeedLimit, isRestPath = p.IsRestPath, restCapacity = p.RestCapacity, minFollowingDistanceMeters = p.MinFollowingDistanceMeters }).ToListAsync();
    return Ok(paths);
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("{mapVersionId}/paths")]
public async Task<IActionResult> CreatePath(Guid mapVersionId, [FromBody] PathRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var from = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.NodeId == req.FromNodeId && x.MapVersionId == mapVersionId);
        var to = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.NodeId == req.ToNodeId && x.MapVersionId == mapVersionId);
        if (from == null || to == null) return BadRequest(new { error = "nodes not found" });
        var line = new LineString(new[] { from.Location.Coordinate, to.Location.Coordinate }) { SRID = 0 };
        var p = new MapPath { PathId = Guid.NewGuid(), MapVersionId = mapVersionId, FromNodeId = req.FromNodeId, ToNodeId = req.ToNodeId, Location = line, TwoWay = req.TwoWay, IsActive = req.IsActive, SpeedLimit = req.SpeedLimit, IsRestPath = req.IsRestPath, RestCapacity = req.RestCapacity };
        await db.Paths.AddAsync(p);
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "path", id = p.PathId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g2) ? g2 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.path.create", TargetType = "map", TargetId = p.PathId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(p);
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPut("{mapVersionId}/paths/{pathId}")]
public async Task<IActionResult> UpdatePath(Guid mapVersionId, Guid pathId, [FromBody] PathRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var p = await db.Paths.FirstOrDefaultAsync(x => x.PathId == pathId && x.MapVersionId == mapVersionId);
        if (p == null) return NotFound();
        var from = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.NodeId == req.FromNodeId && x.MapVersionId == mapVersionId);
        var to = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.NodeId == req.ToNodeId && x.MapVersionId == mapVersionId);
        if (from == null || to == null) return BadRequest(new { error = "nodes not found" });
        p.FromNodeId = req.FromNodeId;
        p.ToNodeId = req.ToNodeId;
        p.Location = new LineString(new[] { from.Location.Coordinate, to.Location.Coordinate }) { SRID = 0 };
        p.TwoWay = req.TwoWay;
        p.IsActive = req.IsActive;
        p.SpeedLimit = req.SpeedLimit;
        p.IsRestPath = req.IsRestPath;
        p.RestCapacity = req.RestCapacity;
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "path", id = p.PathId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g3) ? g3 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.path.update", TargetType = "map", TargetId = p.PathId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(p);
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPut("{mapVersionId}/paths/{pathId}/maintenance")]
public async Task<IActionResult> TogglePathMaintenance(Guid mapVersionId, Guid pathId, [FromBody] bool isMaintenance, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var p = await db.Paths.FirstOrDefaultAsync(x => x.PathId == pathId && x.MapVersionId == mapVersionId);
        if (p == null) return NotFound();
        p.IsMaintenance = isMaintenance;
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "path", id = p.PathId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g4) ? g4 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.path.maintenance", TargetType = "map", TargetId = p.PathId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPut("{mapVersionId}/paths/{pathId}/rest")]
public async Task<IActionResult> UpdateRestPath(Guid mapVersionId, Guid pathId, [FromBody] int capacity, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var p = await db.Paths.FirstOrDefaultAsync(x => x.PathId == pathId && x.MapVersionId == mapVersionId);
        if (p == null) return NotFound();
        p.IsRestPath = capacity > 0;
        p.RestCapacity = capacity;
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "path", id = p.PathId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g5) ? g5 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.path.rest", TargetType = "map", TargetId = p.PathId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
}

[Authorize]
[HttpGet("{mapVersionId}/points")]
public async Task<IActionResult> ListPoints(Guid mapVersionId, [FromServices] AppDbContext db)
{
    var points = await db.Points.AsNoTracking().Where(p => p.MapVersionId == mapVersionId).Select(p => new { pointId = p.PointId, name = p.Name, type = p.Type, x = p.Location.X, y = p.Location.Y, attachedNodeId = p.AttachedNodeId }).ToListAsync();
    return Ok(points);
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("{mapVersionId}/points")]
public async Task<IActionResult> CreatePoint(Guid mapVersionId, [FromBody] PointRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var pt = new MapPoint { PointId = Guid.NewGuid(), MapVersionId = mapVersionId, Name = req.Name, Type = req.Type, Location = new Point(req.X, req.Y) { SRID = 0 }, AttachedNodeId = req.AttachedNodeId };
        await db.Points.AddAsync(pt);
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "point", id = pt.PointId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g6) ? g6 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.point.create", TargetType = "map", TargetId = pt.PointId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(pt);
    }

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPut("{mapVersionId}/points/{pointId}")]
public async Task<IActionResult> UpdatePoint(Guid mapVersionId, Guid pointId, [FromBody] PointRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var pt = await db.Points.FirstOrDefaultAsync(x => x.PointId == pointId && x.MapVersionId == mapVersionId);
        if (pt == null) return NotFound();
        pt.Name = req.Name;
        pt.Type = req.Type;
        pt.Location = new Point(req.X, req.Y) { SRID = 0 };
        pt.AttachedNodeId = req.AttachedNodeId;
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "point", id = pt.PointId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g7) ? g7 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.point.update", TargetType = "map", TargetId = pt.PointId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(pt);
}

[Authorize]
[HttpGet("{mapVersionId}/qrs")]
public async Task<IActionResult> ListQrs(Guid mapVersionId, [FromServices] AppDbContext db)
{
    var qrs = await db.QrAnchors.AsNoTracking().Where(q => q.MapVersionId == mapVersionId).Select(q => new { qrId = q.QrId, qrCode = q.QrCode, x = q.Location.X, y = q.Location.Y, pathId = q.PathId, distanceAlongPath = q.DistanceAlongPath }).ToListAsync();
    return Ok(qrs);
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("{mapVersionId}/qrs")]
public async Task<IActionResult> CreateQr(Guid mapVersionId, [FromBody] QrRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var qr = new QrAnchor { QrId = Guid.NewGuid(), MapVersionId = mapVersionId, QrCode = req.QrCode, Location = new Point(req.X, req.Y) { SRID = 0 }, PathId = req.PathId, DistanceAlongPath = req.DistanceAlongPath };
        await db.QrAnchors.AddAsync(qr);
        await db.SaveChangesAsync();
        await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "qr", id = qr.QrId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g8) ? g8 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.qr.create", TargetType = "map", TargetId = qr.QrId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(qr);
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPut("{mapVersionId}/qrs/{qrId}")]
public async Task<IActionResult> UpdateQr(Guid mapVersionId, Guid qrId, [FromBody] QrRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<BackendV2.Api.Hub.RealtimeHub> hub)
{
        var qr = await db.QrAnchors.FirstOrDefaultAsync(x => x.QrId == qrId && x.MapVersionId == mapVersionId);
        if (qr == null) return NotFound();
        qr.QrCode = req.QrCode;
        qr.Location = new Point(req.X, req.Y) { SRID = 0 };
        qr.PathId = req.PathId;
        qr.DistanceAlongPath = req.DistanceAlongPath;
        await db.SaveChangesAsync();
    await hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(SignalRTopics.MapEntityUpdated, new object[] { new { type = "qr", id = qr.QrId.ToString(), mapVersionId = mapVersionId.ToString() } }, System.Threading.CancellationToken.None);
        var actor = User.FindFirst("sub")?.Value;
        System.Guid? actorId = System.Guid.TryParse(actor, out var g9) ? g9 : null;
        await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.qr.update", TargetType = "map", TargetId = qr.QrId.ToString(), Outcome = "OK", DetailsJson = "{}" });
        await db.SaveChangesAsync();
        return Ok(qr);
}

[Authorize(Policy = BackendV2.Api.Infrastructure.Security.AuthorizationPolicies.Planner)]
[HttpPost("{mapVersionId}/validate")]
public async Task<IActionResult> Validate(Guid mapVersionId, [FromServices] AppDbContext db)
{
    var nodes = await db.Nodes.AsNoTracking().Where(n => n.MapVersionId == mapVersionId && n.IsActive).ToListAsync();
    var paths = await db.Paths.AsNoTracking().Where(p => p.MapVersionId == mapVersionId && p.IsActive).ToListAsync();
    var points = await db.Points.AsNoTracking().Where(p => p.MapVersionId == mapVersionId).ToListAsync();
    var qrs = await db.QrAnchors.AsNoTracking().Where(q => q.MapVersionId == mapVersionId).ToListAsync();
    var adj = new System.Collections.Generic.Dictionary<Guid, System.Collections.Generic.List<Guid>>();
    foreach (var p in paths.Where(p => !p.IsMaintenance))
    {
        if (!adj.ContainsKey(p.FromNodeId)) adj[p.FromNodeId] = new System.Collections.Generic.List<Guid>();
        adj[p.FromNodeId].Add(p.ToNodeId);
        if (p.TwoWay)
        {
            if (!adj.ContainsKey(p.ToNodeId)) adj[p.ToNodeId] = new System.Collections.Generic.List<Guid>();
            adj[p.ToNodeId].Add(p.FromNodeId);
        }
    }
    var visited = new System.Collections.Generic.HashSet<Guid>();
    if (nodes.Count > 0)
    {
        var start = nodes[0].NodeId;
        var queue = new System.Collections.Generic.Queue<Guid>();
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            if (!adj.ContainsKey(u)) continue;
            foreach (var v in adj[u])
            {
                if (!visited.Contains(v))
                {
                    visited.Add(v);
                    queue.Enqueue(v);
                }
            }
        }
    }
    var unreachablePoints = points.Where(pt => pt.AttachedNodeId != null && !visited.Contains(pt.AttachedNodeId.Value)).Select(pt => new { pointId = pt.PointId, name = pt.Name }).ToList();
    var danglingNodes = nodes.Where(n => !paths.Any(p => p.FromNodeId == n.NodeId || p.ToNodeId == n.NodeId)).Select(n => new { nodeId = n.NodeId, name = n.Name }).ToList();
    var qrIssues = new System.Collections.Generic.List<object>();
    foreach (var qr in qrs)
    {
        var path = paths.FirstOrDefault(p => p.PathId == qr.PathId);
        if (path == null)
        {
            qrIssues.Add(new { qrId = qr.QrId, issue = "path_not_found" });
            continue;
        }
        var dist = path.Location.Distance(qr.Location);
        if (dist > 1.0) qrIssues.Add(new { qrId = qr.QrId, issue = "qr_far_from_path", distance = dist });
        var lineLen = path.Location.Length;
        if (qr.DistanceAlongPath < 0 || qr.DistanceAlongPath > lineLen) qrIssues.Add(new { qrId = qr.QrId, issue = "distance_along_out_of_range", max = lineLen });
    }
    var maintenanceImpact = unreachablePoints.Select(p => p.pointId).ToList();
    var result = new { danglingNodes, unreachablePoints, qrIssues, maintenanceImpact };
    var actor = User.FindFirst("sub")?.Value;
    System.Guid? actorId = System.Guid.TryParse(actor, out var g10) ? g10 : null;
    await db.AuditEvents.AddAsync(new BackendV2.Api.Model.Ops.AuditEvent { AuditEventId = System.Guid.NewGuid(), Timestamp = System.DateTimeOffset.UtcNow, ActorUserId = actorId, Action = "map.validate", TargetType = "map", TargetId = mapVersionId.ToString(), Outcome = "OK", DetailsJson = "{}" });
    await db.SaveChangesAsync();
    return Ok(result);
}
}
