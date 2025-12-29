using Backend.Dto;
using Backend.Model;
using NetTopologySuite.Geometries;
using System.Linq;

namespace Backend.Mapping;

public static class PathMapper
{
    public static PathDto ToDto(Paths entity)
    {
        var dto = new PathDto
        {
            Id = entity.Id,
            MapId = entity.MapId,
            StartNodeId = entity.StartNodeId,
            EndNodeId = entity.EndNodeId,
            TwoWay = entity.TwoWay,
            Length = entity.Length,
            Status = entity.Status,
            Points = entity.Location != null
                ? entity.Location.Coordinates.Select(c => new PathPointDto { X = c.X, Y = c.Y }).ToList()
                : null
        };
        return dto;
    }

    public static Paths ToEntity(PathDto dto)
    {
        var entity = new Paths
        {
            Id = dto.Id,
            MapId = dto.MapId,
            StartNodeId = dto.StartNodeId,
            EndNodeId = dto.EndNodeId,
            TwoWay = dto.TwoWay,
            Length = dto.Length,
            Status = dto.Status
        };

        if (dto.Points != null && dto.Points.Count > 0)
        {
            var coords = dto.Points.Select(p => new Coordinate(p.X, p.Y)).ToArray();
            entity.Location = new LineString(coords) { SRID = 0 };
        }

        return entity;
    }
}
