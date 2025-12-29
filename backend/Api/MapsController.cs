using Backend.Dto;
using Backend.Endpoints;
using Backend.Infrastructure.Persistence;
using Backend.Mapping;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api;

[ApiController]
public class MapsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MapsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet(ApiRoutes.Maps)]
    public async Task<ActionResult<IEnumerable<MapDto>>> GetAll(CancellationToken ct)
    {
        var entities = await _db.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Paths)
            .Include(m => m.Points)
            .Include(m => m.Qrs)
            .AsNoTracking()
            .ToListAsync(ct);
        var dtos = entities.Select(MapMapper.ToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet(ApiRoutes.MapsById)]
    public async Task<ActionResult<MapDto>> GetById(int id, CancellationToken ct)
    {
        var entity = await _db.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Paths)
            .Include(m => m.Points)
            .Include(m => m.Qrs)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity == null) return NotFound();
        return Ok(MapMapper.ToDto(entity));
    }

    [HttpPost(ApiRoutes.Maps)]
    public async Task<ActionResult<MapDto>> Create([FromBody] MapDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        var map = MapMapper.ToEntity(dto);
        map.Id = 0;
        _db.Maps.Add(map);
        await _db.SaveChangesAsync(ct);

        if (dto.Nodes != null)
        {
            foreach (var n in dto.Nodes)
            {
                n.MapId = map.Id;
                var node = NodeMapper.ToEntity(n);
                node.Id = 0;
                _db.Nodes.Add(node);
            }
            await _db.SaveChangesAsync(ct);
        }

        if (dto.Paths != null)
        {
            foreach (var p in dto.Paths)
            {
                p.MapId = map.Id;
                var path = PathMapper.ToEntity(p);
                path.Id = 0;
                _db.Paths.Add(path);
            }
            await _db.SaveChangesAsync(ct);
        }

        if (dto.Points != null)
        {
            foreach (var mp in dto.Points)
            {
                mp.MapId = map.Id;
                var point = MapPointMapper.ToEntity(mp);
                point.Id = 0;
                _db.Points.Add(point);
            }
            await _db.SaveChangesAsync(ct);
        }

        if (dto.Qrs != null)
        {
            foreach (var q in dto.Qrs)
            {
                q.MapId = map.Id;
                var qr = QrMapper.ToEntity(q);
                qr.Id = 0;
                _db.Qrs.Add(qr);
            }
            await _db.SaveChangesAsync(ct);
        }

        var createdEntity = await _db.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Paths)
            .Include(m => m.Points)
            .Include(m => m.Qrs)
            .AsNoTracking()
            .FirstAsync(m => m.Id == map.Id, ct);
        var createdDto = MapMapper.ToDto(createdEntity);
        return Created($"{ApiRoutes.Maps}/{createdDto.Id}", createdDto);
    }

    [HttpPut(ApiRoutes.MapsById)]
    public async Task<ActionResult<MapDto>> Update(int id, [FromBody] MapDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest("Route id and body id must match");
        var map = await _db.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Paths)
            .Include(m => m.Points)
            .Include(m => m.Qrs)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
        if (map == null) return NotFound();
        map.Name = dto.Name;

        if (map.Qrs != null && map.Qrs.Count > 0)
        {
            _db.Qrs.RemoveRange(map.Qrs);
            await _db.SaveChangesAsync(ct);
        }
        if (map.Paths != null && map.Paths.Count > 0)
        {
            _db.Paths.RemoveRange(map.Paths);
            await _db.SaveChangesAsync(ct);
        }
        if (map.Nodes != null && map.Nodes.Count > 0)
        {
            _db.Nodes.RemoveRange(map.Nodes);
            await _db.SaveChangesAsync(ct);
        }
        if (map.Points != null && map.Points.Count > 0)
        {
            _db.Points.RemoveRange(map.Points);
            await _db.SaveChangesAsync(ct);
        }

        if (dto.Nodes != null)
        {
            foreach (var n in dto.Nodes)
            {
                n.MapId = map.Id;
                var node = NodeMapper.ToEntity(n);
                node.Id = 0;
                _db.Nodes.Add(node);
            }
        }
        if (dto.Paths != null)
        {
            foreach (var p in dto.Paths)
            {
                p.MapId = map.Id;
                var path = PathMapper.ToEntity(p);
                path.Id = 0;
                _db.Paths.Add(path);
            }
        }
        if (dto.Points != null)
        {
            foreach (var mp in dto.Points)
            {
                mp.MapId = map.Id;
                var point = MapPointMapper.ToEntity(mp);
                point.Id = 0;
                _db.Points.Add(point);
            }
        }
        if (dto.Qrs != null)
        {
            foreach (var q in dto.Qrs)
            {
                q.MapId = map.Id;
                var qr = QrMapper.ToEntity(q);
                qr.Id = 0;
                _db.Qrs.Add(qr);
            }
        }

        await _db.SaveChangesAsync(ct);

        var entity = await _db.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Paths)
            .Include(m => m.Points)
            .Include(m => m.Qrs)
            .AsNoTracking()
            .FirstAsync(m => m.Id == map.Id, ct);
        return Ok(MapMapper.ToDto(entity));
    }

    [HttpDelete(ApiRoutes.MapsById)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var map = await _db.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Paths)
            .Include(m => m.Points)
            .Include(m => m.Qrs)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
        if (map == null) return NotFound();
        _db.Maps.Remove(map);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
