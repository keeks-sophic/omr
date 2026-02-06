using BackendV3.Endpoints;
using BackendV3.Infrastructure.Security;
using BackendV3.Modules.Maps.Dto.Requests;
using BackendV3.Modules.Maps.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV3.Modules.Maps.Api;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Viewer)]
public sealed class MapsController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Maps.Base)]
    public async Task<IActionResult> CreateMap(
        [FromBody] CreateMapRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var actor = MapManagementService.GetActorUserId(User);
        var dto = await maps.CreateMapAsync(req, actor, ct);
        return dto == null ? BadRequest() : Ok(dto);
    }

    [HttpGet(ApiRoutes.Maps.Base)]
    public async Task<IActionResult> ListMaps(
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var list = await maps.ListMapsAsync(ct);
        return Ok(list);
    }

    [HttpGet(ApiRoutes.Maps.MapById)]
    public async Task<IActionResult> GetMap(
        Guid mapId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var dto = await maps.GetMapAsync(mapId, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet(ApiRoutes.Maps.Versions)]
    public async Task<IActionResult> ListVersions(
        Guid mapId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var list = await maps.ListVersionsAsync(mapId, ct);
        return list == null ? NotFound() : Ok(list);
    }

    [HttpGet(ApiRoutes.Maps.VersionById)]
    public async Task<IActionResult> GetVersion(
        Guid mapId,
        Guid mapVersionId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var dto = await maps.GetVersionAsync(mapId, mapVersionId, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpGet(ApiRoutes.Maps.Draft)]
    public async Task<IActionResult> GetOrCreateDraft(
        Guid mapId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var actor = MapManagementService.GetActorUserId(User);
        var dto = await maps.GetOrCreateDraftAsync(mapId, actor, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Maps.Clone)]
    public async Task<IActionResult> CloneVersion(
        Guid mapId,
        Guid mapVersionId,
        [FromBody] CloneMapRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var actor = MapManagementService.GetActorUserId(User);
        var dto = await maps.CloneVersionAsync(mapId, mapVersionId, req, actor, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Maps.Publish)]
    public async Task<IActionResult> PublishMap(
        Guid mapId,
        Guid mapVersionId,
        [FromBody] PublishMapRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var actor = MapManagementService.GetActorUserId(User);
        var ok = await maps.PublishVersionAsync(mapId, mapVersionId, req, actor, ct);
        return ok ? Ok(new { ok = true }) : NotFound();
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Maps.Activate)]
    public async Task<IActionResult> ActivatePublishedVersion(
        Guid mapId,
        Guid mapVersionId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var ok = await maps.SetActivePublishedVersionAsync(mapId, mapVersionId, ct);
        return ok ? Ok(new { ok = true }) : NotFound();
    }

    [HttpGet(ApiRoutes.Maps.Nodes)]
    public async Task<IActionResult> ListNodes(
        Guid mapId,
        Guid mapVersionId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var nodes = await maps.ListNodesAsync(mapId, mapVersionId, ct);
        return nodes == null ? NotFound() : Ok(nodes);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Maps.Nodes)]
    public async Task<IActionResult> CreateNode(
        Guid mapId,
        Guid mapVersionId,
        [FromBody] CreateNodeRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var node = await maps.CreateNodeAsync(mapId, mapVersionId, req, ct);
        return node == null ? BadRequest() : Ok(node);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Maps.NodeById)]
    public async Task<IActionResult> UpdateNode(
        Guid mapId,
        Guid mapVersionId,
        Guid nodeId,
        [FromBody] UpdateNodeRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var node = await maps.UpdateNodeAsync(mapId, mapVersionId, nodeId, req, ct);
        return node == null ? NotFound() : Ok(node);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Maps.NodeMaintenance)]
    public async Task<IActionResult> SetNodeMaintenance(
        Guid mapId,
        Guid mapVersionId,
        Guid nodeId,
        [FromBody] SetMaintenanceRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var ok = await maps.SetNodeMaintenanceAsync(mapId, mapVersionId, nodeId, req.IsMaintenance, ct);
        return ok ? Ok(new { ok = true }) : NotFound();
    }

    [HttpGet(ApiRoutes.Maps.Paths)]
    public async Task<IActionResult> ListPaths(
        Guid mapId,
        Guid mapVersionId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var paths = await maps.ListPathsAsync(mapId, mapVersionId, ct);
        return paths == null ? NotFound() : Ok(paths);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Maps.Paths)]
    public async Task<IActionResult> CreatePath(
        Guid mapId,
        Guid mapVersionId,
        [FromBody] CreatePathRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var path = await maps.CreatePathAsync(mapId, mapVersionId, req, ct);
        return path == null ? BadRequest() : Ok(path);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Maps.PathById)]
    public async Task<IActionResult> UpdatePath(
        Guid mapId,
        Guid mapVersionId,
        Guid pathId,
        [FromBody] UpdatePathRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var path = await maps.UpdatePathAsync(mapId, mapVersionId, pathId, req, ct);
        return path == null ? NotFound() : Ok(path);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Maps.PathMaintenance)]
    public async Task<IActionResult> SetPathMaintenance(
        Guid mapId,
        Guid mapVersionId,
        Guid pathId,
        [FromBody] SetMaintenanceRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var ok = await maps.SetPathMaintenanceAsync(mapId, mapVersionId, pathId, req.IsMaintenance, ct);
        return ok ? Ok(new { ok = true }) : NotFound();
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Maps.PathRest)]
    public async Task<IActionResult> SetPathRest(
        Guid mapId,
        Guid mapVersionId,
        Guid pathId,
        [FromBody] SetRestOptionsRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var path = await maps.SetPathRestAsync(mapId, mapVersionId, pathId, req, ct);
        return path == null ? NotFound() : Ok(path);
    }

    [HttpGet(ApiRoutes.Maps.Points)]
    public async Task<IActionResult> ListPoints(
        Guid mapId,
        Guid mapVersionId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var points = await maps.ListPointsAsync(mapId, mapVersionId, ct);
        return points == null ? NotFound() : Ok(points);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Maps.Points)]
    public async Task<IActionResult> CreatePoint(
        Guid mapId,
        Guid mapVersionId,
        [FromBody] CreatePointRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var point = await maps.CreatePointAsync(mapId, mapVersionId, req, ct);
        return point == null ? BadRequest() : Ok(point);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Maps.PointById)]
    public async Task<IActionResult> UpdatePoint(
        Guid mapId,
        Guid mapVersionId,
        Guid pointId,
        [FromBody] UpdatePointRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var point = await maps.UpdatePointAsync(mapId, mapVersionId, pointId, req, ct);
        return point == null ? NotFound() : Ok(point);
    }

    [HttpGet(ApiRoutes.Maps.Qrs)]
    public async Task<IActionResult> ListQrs(
        Guid mapId,
        Guid mapVersionId,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var qrs = await maps.ListQrsAsync(mapId, mapVersionId, ct);
        return qrs == null ? NotFound() : Ok(qrs);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPost(ApiRoutes.Maps.Qrs)]
    public async Task<IActionResult> CreateQr(
        Guid mapId,
        Guid mapVersionId,
        [FromBody] CreateQrRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var qr = await maps.CreateQrAsync(mapId, mapVersionId, req, ct);
        return qr == null ? BadRequest() : Ok(qr);
    }

    [Authorize(Policy = AuthorizationPolicies.Operator)]
    [HttpPut(ApiRoutes.Maps.QrById)]
    public async Task<IActionResult> UpdateQr(
        Guid mapId,
        Guid mapVersionId,
        Guid qrId,
        [FromBody] UpdateQrRequest req,
        [FromServices] MapManagementService maps,
        CancellationToken ct)
    {
        var qr = await maps.UpdateQrAsync(mapId, mapVersionId, qrId, req, ct);
        return qr == null ? NotFound() : Ok(qr);
    }

    [HttpGet(ApiRoutes.Maps.Snapshot)]
    public async Task<IActionResult> Snapshot(
        Guid mapId,
        Guid mapVersionId,
        [FromServices] MapSnapshotService snapshots,
        CancellationToken ct)
    {
        var snapshot = await snapshots.GetSnapshotAsync(mapId, mapVersionId, ct);
        return snapshot == null ? NotFound() : Ok(snapshot);
    }
}
