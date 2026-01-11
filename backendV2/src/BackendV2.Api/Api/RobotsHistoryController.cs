using System;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/robots/{robotId}/history")]
public class RobotsHistoryController : ControllerBase
{
    [Authorize]
    [HttpGet("state")]
    public async Task<IActionResult> GetStateHistory(string robotId, [FromQuery] DateTimeOffset? fromTime, [FromQuery] DateTimeOffset? toTime, [FromQuery] int? limit, [FromServices] AppDbContext db)
    {
        var q = db.RobotEvents.AsNoTracking().Where(e => e.RobotId == robotId && (e.Type == "state.snapshot" || e.Type == "state.event"));
        if (fromTime != null) q = q.Where(e => e.Timestamp >= fromTime);
        if (toTime != null) q = q.Where(e => e.Timestamp <= toTime);
        var list = await q.OrderByDescending(e => e.Timestamp).Take(limit ?? 200).Select(e => new { timestamp = e.Timestamp, type = e.Type, payload = e.Payload }).ToListAsync();
        return Ok(list);
    }

    [Authorize]
    [HttpGet("telemetry")]
    public async Task<IActionResult> GetTelemetryHistory(string robotId, [FromQuery] string type, [FromQuery] DateTimeOffset? fromTime, [FromQuery] DateTimeOffset? toTime, [FromQuery] int? limit, [FromServices] AppDbContext db)
    {
        var pref = $"telemetry.{type}";
        var q = db.RobotEvents.AsNoTracking().Where(e => e.RobotId == robotId && e.Type.StartsWith(pref));
        if (fromTime != null) q = q.Where(e => e.Timestamp >= fromTime);
        if (toTime != null) q = q.Where(e => e.Timestamp <= toTime);
        var list = await q.OrderByDescending(e => e.Timestamp).Take(limit ?? 200).Select(e => new { timestamp = e.Timestamp, type = e.Type, payload = e.Payload }).ToListAsync();
        return Ok(list);
    }

    [Authorize]
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(string robotId, [FromQuery] DateTimeOffset? fromTime, [FromQuery] DateTimeOffset? toTime, [FromQuery] int? limit, [FromServices] AppDbContext db)
    {
        var q = db.RobotEvents.AsNoTracking().Where(e => e.RobotId == robotId && e.Type == "log.event");
        if (fromTime != null) q = q.Where(e => e.Timestamp >= fromTime);
        if (toTime != null) q = q.Where(e => e.Timestamp <= toTime);
        var list = await q.OrderByDescending(e => e.Timestamp).Take(limit ?? 200).Select(e => new { timestamp = e.Timestamp, type = e.Type, payload = e.Payload }).ToListAsync();
        return Ok(list);
    }

    [Authorize]
    [HttpGet("telemetry/{channel}")]
    public async Task<IActionResult> GetTelemetrySeries(string robotId, string channel, [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, [FromQuery] int downsample, [FromServices] BackendV2.Api.Service.Timescale.TimescaleQueryService ts)
    {
        if (downsample <= 0) downsample = 1000;
        if (string.Equals(channel, "battery", StringComparison.OrdinalIgnoreCase))
        {
            var points = await ts.GetBatteryAsync(robotId, from, to, downsample);
            return Ok(points.Select(p => new { timestamp = p.Ts, batteryPct = p.V1, voltage = p.V2 }));
        }
        if (string.Equals(channel, "motion", StringComparison.OrdinalIgnoreCase))
        {
            var points = await ts.GetMotionAsync(robotId, from, to, downsample);
            return Ok(points.Select(p => new { timestamp = p.Ts, currentLinearVel = p.V1, targetLinearVel = p.V2 }));
        }
        if (string.Equals(channel, "pose", StringComparison.OrdinalIgnoreCase))
        {
            var points = await ts.GetPoseAsync(robotId, from, to, downsample);
            return Ok(points.Select(p => new { timestamp = p.Ts, x = p.V1, y = p.V2, heading = p.V3 }));
        }
        return BadRequest(new { message = "unsupported_channel" });
    }
}
