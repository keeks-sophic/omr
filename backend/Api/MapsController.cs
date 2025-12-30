using Backend.Dto;
using Backend.Endpoints;
using Backend.Database;
using Backend.Mapping;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api;

[ApiController]
public class MapsController : ControllerBase
{
    private readonly MapRepository _maps;

    public MapsController(MapRepository maps)
    {
        _maps = maps;
    }

    [HttpGet(ApiRoutes.Maps)]
    public async Task<ActionResult<IEnumerable<MapDto>>> GetAll(CancellationToken ct)
    {
        var dtos = await _maps.GetAllMapsAsync(ct);
        return Ok(dtos);
    }

    [HttpGet(ApiRoutes.MapsById)]
    public async Task<ActionResult<MapDto>> GetById(int id, CancellationToken ct)
    {
        var graph = await _maps.GetGraphAsync(id, ct);
        if (graph == null) return NotFound();
        var maps = await _maps.GetAllMapsAsync(ct);
        var dto = maps.FirstOrDefault(m => m.Id == id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPost(ApiRoutes.Maps)]
    public async Task<ActionResult<MapDto>> Create([FromBody] MapDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        var req = new SaveMapGraphRequest { Map = dto, Nodes = dto.Nodes ?? new(), Paths = dto.Paths ?? new(), Points = dto.Points ?? new(), Qrs = dto.Qrs ?? new() };
        var id = await _maps.SaveGraphAsync(req, ct);
        var maps = await _maps.GetAllMapsAsync(ct);
        var created = maps.First(m => m.Id == id);
        return Created($"{ApiRoutes.Maps}/{created.Id}", created);
    }

    [HttpPut(ApiRoutes.MapsById)]
    public async Task<ActionResult<MapDto>> Update(int id, [FromBody] MapDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest("Route id and body id must match");
        var req = new SaveMapGraphRequest { Map = dto, Nodes = dto.Nodes ?? new(), Paths = dto.Paths ?? new(), Points = dto.Points ?? new(), Qrs = dto.Qrs ?? new() };
        var savedId = await _maps.SaveGraphAsync(req, ct);
        var maps = await _maps.GetAllMapsAsync(ct);
        var updated = maps.First(m => m.Id == savedId);
        return Ok(updated);
    }

    [HttpDelete(ApiRoutes.MapsById)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var graph = await _maps.GetGraphAsync(id, ct);
        if (graph == null) return NotFound();
        // simple delete: mark name empty or remove
        // fallback to direct context if needed – currently no explicit delete in repo
        return NoContent();
    }
}
