using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Backend.SignalR;
using Backend.Dto;
using Backend.Service;
using Backend.Topics;
using Microsoft.Extensions.Options;

namespace Backend.Api;

[ApiController]
public class RobotsController : ControllerBase
{
    private readonly RobotRepository _repo;
    private readonly IHubContext<RobotsHub> _hub;
    private readonly IRoutePlanQueue _routeQueue;
    private readonly NatsService _nats;
    private readonly IOptions<NatsOptions> _opts;
    public RobotsController(RobotRepository repo, IHubContext<RobotsHub> hub, IRoutePlanQueue routeQueue, NatsService nats, IOptions<NatsOptions> opts) { _repo = repo; _hub = hub; _routeQueue = routeQueue; _nats = nats; _opts = opts; }

    [HttpGet("/robots")]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(CancellationToken ct)
    {
        var list = await _repo.GetAllRobotsAsync(ct);
        return Ok(list);
    }

    [HttpGet("/robots/unassigned")]
    public async Task<ActionResult<IEnumerable<object>>> GetUnassigned(CancellationToken ct)
    {
        var list = await _repo.GetUnassignedRobotsAsync(ct);
        return Ok(list);
    }

    [HttpGet("/maps/{mapId}/robots")]
    public async Task<ActionResult<IEnumerable<object>>> GetByMap(int mapId, CancellationToken ct)
    {
        var list = await _repo.GetRobotsByMapAsync(mapId, ct);
        return Ok(list);
    }

    public class AssignRequest { public int MapId { get; set; } }
    [HttpPost("/robots/{ip}/assign")]
    public async Task<ActionResult<object>> Assign(string ip, [FromBody] AssignRequest req, CancellationToken ct)
    {
        var rob = await _repo.AssignRobotToMapAsync(ip, req.MapId, ct);
        if (rob == null) return NotFound();
        var payload = new
        {
            name = rob.Name,
            ip = rob.Ip,
            x = rob.X ?? 0,
            y = rob.Y ?? 0,
            state = rob.State,
            battery = rob.Battery,
            connected = rob.Connected,
            lastActive = rob.LastActive,
            mapId = rob.MapId
        };
        await _hub.Clients.All.SendAsync(SignalRTopics.Telemetry, payload, ct);
        if (rob.MapId.HasValue) await _hub.Clients.Group($"map:{rob.MapId.Value}").SendAsync(SignalRTopics.Telemetry, payload, ct);
        return Ok(new { name = rob.Name, ip = rob.Ip, mapId = rob.MapId, x = rob.X, y = rob.Y });
    }

    [HttpDelete("/robots/{ip}/assign")]
    public async Task<ActionResult<object>> Unassign(string ip, CancellationToken ct)
    {
        var rob = await _repo.UnassignRobotAsync(ip, ct);
        if (rob == null) return NotFound();
        var payload = new
        {
            name = rob.Name,
            ip = rob.Ip,
            x = rob.X ?? 0,
            y = rob.Y ?? 0,
            state = rob.State,
            battery = rob.Battery,
            connected = rob.Connected,
            lastActive = rob.LastActive,
            mapId = rob.MapId
        };
        await _hub.Clients.All.SendAsync(SignalRTopics.Telemetry, payload, ct);
        return Ok(new { name = rob.Name, ip = rob.Ip, mapId = rob.MapId });
    }

    public class RelocateRequest { public double X { get; set; } public double Y { get; set; } }
    [HttpPut("/robots/{ip}/relocate")]
    public async Task<ActionResult<object>> Relocate(string ip, [FromBody] RelocateRequest req, CancellationToken ct)
    {
        var rob = await _repo.RelocateRobotAsync(ip, req.X, req.Y, ct);
        if (rob == null) return NotFound();
        var payload = new
        {
            name = rob.Name,
            ip = rob.Ip,
            x = rob.X ?? 0,
            y = rob.Y ?? 0,
            state = rob.State,
            battery = rob.Battery,
            connected = rob.Connected,
            lastActive = rob.LastActive,
            mapId = rob.MapId
        };
        await _hub.Clients.All.SendAsync(SignalRTopics.Telemetry, payload, ct);
        if (rob.MapId.HasValue) await _hub.Clients.Group($"map:{rob.MapId.Value}").SendAsync(SignalRTopics.Telemetry, payload, ct);
        await _nats.ConnectAsync(_opts.Value.Url, ct);
        await _nats.EnsureStreamAsync(_opts.Value.ControlWildcardStream ?? "ROBOTS_CONTROL", $"{NatsTopics.SyncPrefix}.>");
        var sync = new
        {
            command = NatsTopics.CommandRobotSync,
            ip = rob.Ip,
            robot = new
            {
                id = rob.Id,
                name = rob.Name,
                mapId = rob.MapId,
                x = rob.X,
                y = rob.Y,
                battery = rob.Battery,
                state = rob.State
            }
        };
        await _nats.PublishAsync($"{NatsTopics.SyncPrefix}.{rob.Id}", sync, ct);
        await _nats.PublishAsync($"{NatsTopics.SyncPrefix}.{ip}", sync, ct);
        return Ok(new { name = rob.Name, ip = rob.Ip, x = rob.X, y = rob.Y, mapId = rob.MapId });
    }

    [HttpPost("/robots/{ip}/move")]
    public async Task<ActionResult> Move(string ip, CancellationToken ct)
    {
        await _repo.MoveRobotAsync(ip, ct);
        return Accepted();
    }

    [HttpPost("/robots/{ip}/navigate")]
    public async Task<ActionResult> Navigate(string ip, [FromBody] RoutePlanRequest req, CancellationToken ct)
    {
        await _routeQueue.EnqueueAsync(new RoutePlanTask { Ip = ip, MapId = req.MapId, X = req.X, Y = req.Y }, ct);
        return Accepted();
    }
}
