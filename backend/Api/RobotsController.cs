using Backend.Database;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api;

[ApiController]
public class RobotsController : ControllerBase
{
    private readonly RobotRepository _repo;
    public RobotsController(RobotRepository repo) { _repo = repo; }

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
        return Ok(new { name = rob.Name, ip = rob.Ip, mapId = rob.MapId, x = rob.X, y = rob.Y });
    }

    [HttpDelete("/robots/{ip}/assign")]
    public async Task<ActionResult<object>> Unassign(string ip, CancellationToken ct)
    {
        var rob = await _repo.UnassignRobotAsync(ip, ct);
        if (rob == null) return NotFound();
        return Ok(new { name = rob.Name, ip = rob.Ip, mapId = rob.MapId });
    }

    public class RelocateRequest { public double X { get; set; } public double Y { get; set; } }
    [HttpPut("/robots/{ip}/relocate")]
    public async Task<ActionResult<object>> Relocate(string ip, [FromBody] RelocateRequest req, CancellationToken ct)
    {
        var rob = await _repo.RelocateRobotAsync(ip, req.X, req.Y, ct);
        if (rob == null) return NotFound();
        return Ok(new { name = rob.Name, ip = rob.Ip, x = rob.X, y = rob.Y, mapId = rob.MapId });
    }

    [HttpPost("/robots/{ip}/move")]
    public async Task<ActionResult> Move(string ip, CancellationToken ct)
    {
        await _repo.MoveRobotAsync(ip, ct);
        return Accepted();
    }
}
