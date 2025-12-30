using Backend.Dto;
using Backend.Database;
using Backend.Mapping;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api;

[ApiController]
public class MapGraphsController : ControllerBase
{
    private readonly MapRepository _maps;

    public MapGraphsController(MapRepository maps)
    {
        _maps = maps;
    }

    [HttpGet("/maps")]
    public async Task<ActionResult<IEnumerable<object>>> ListMaps(CancellationToken ct)
    {
        var maps = await _maps.GetAllMapsAsync(ct);
        return Ok(maps.Select(m => new { id = m.Id, name = m.Name }));
    }

    [HttpGet("/maps/{id}/graph")]
    public async Task<ActionResult<object>> GetGraph(int id, CancellationToken ct)
    {
        var graph = await _maps.GetGraphAsync(id, ct);
        if (graph == null) return NotFound();
        return Ok(graph);
    }

    [HttpPost("/maps/graph")]
    public async Task<ActionResult<object>> SaveGraph([FromBody] SaveMapGraphRequest req, CancellationToken ct)
    {
        var id = await _maps.SaveGraphAsync(req, ct);
        return Ok(new { id });
    }
}
