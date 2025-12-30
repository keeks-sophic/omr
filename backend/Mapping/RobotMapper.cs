using Backend.Dto;
using Backend.Model;
using NetTopologySuite.Geometries;

namespace Backend.Mapping;

public static class RobotMapper
{
    public static RobotDto ToDto(Robot entity)
    {
        return new RobotDto
        {
            Id = entity.Id,
            Ip = entity.Ip,
            Name = entity.Name,
            X = entity.Location != null ? entity.Location.X : (entity.X ?? 0),
            Y = entity.Location != null ? entity.Location.Y : (entity.Y ?? 0),
            State = entity.State,
            Battery = entity.Battery,
            Connected = entity.Connected,
            LastActive = entity.LastActive,
            MapId = entity.MapId
        };
    }

    public static Robot ToEntity(RobotDto dto)
    {
        var entity = new Robot
        {
            Id = dto.Id,
            Ip = dto.Ip,
            Name = dto.Name,
            X = dto.X,
            Y = dto.Y,
            State = dto.State,
            Battery = dto.Battery,
            Connected = dto.Connected,
            LastActive = dto.LastActive,
            MapId = dto.MapId
        };

        entity.Location = new Point(dto.X, dto.Y) { SRID = 0 };
        return entity;
    }
}
