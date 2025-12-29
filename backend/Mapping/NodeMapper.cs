using Backend.Dto;
using Backend.Model;
using NetTopologySuite.Geometries;

namespace Backend.Mapping;

public static class NodeMapper
{
    public static NodeDto ToDto(Nodes entity)
    {
        return new NodeDto
        {
            Id = entity.Id,
            MapId = entity.MapId,
            Name = entity.Name,
            X = entity.Location != null ? entity.Location.X : entity.X,
            Y = entity.Location != null ? entity.Location.Y : entity.Y,
            Status = entity.Status
        };
    }

    public static Nodes ToEntity(NodeDto dto)
    {
        var entity = new Nodes
        {
            Id = dto.Id,
            MapId = dto.MapId,
            Name = dto.Name,
            X = dto.X,
            Y = dto.Y,
            Status = dto.Status
        };
        entity.Location = new Point(dto.X, dto.Y) { SRID = 0 };
        return entity;
    }
}
