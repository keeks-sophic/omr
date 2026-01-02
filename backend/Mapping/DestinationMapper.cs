using Backend.Dto;
using Backend.Model;
using NetTopologySuite.Geometries;

namespace Backend.Mapping;

public static class DestinationMapper
{
    public static DestinationDto ToDto(Destinations entity)
    {
        return new DestinationDto
        {
            Id = entity.Id,
            RobotId = entity.RobotId,
            MapId = entity.MapId,
            X = entity.Location != null ? entity.Location.X : entity.X,
            Y = entity.Location != null ? entity.Location.Y : entity.Y
        };
    }

    public static Destinations ToEntity(DestinationDto dto)
    {
        var entity = new Destinations
        {
            Id = dto.Id,
            RobotId = dto.RobotId,
            MapId = dto.MapId,
            X = dto.X,
            Y = dto.Y
        };
        entity.Location = new Point(dto.X, dto.Y) { SRID = 0 };
        return entity;
    }
}

