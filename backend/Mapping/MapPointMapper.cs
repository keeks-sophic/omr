using Backend.Dto;
using Backend.Model;

namespace Backend.Mapping;

public static class MapPointMapper
{
    public static MapPointDto ToDto(Points entity)
    {
        return new MapPointDto
        {
            Id = entity.Id,
            MapId = entity.MapId,
            PathId = entity.PathId,
            Offset = entity.Offset,
            Type = entity.Type,
            Name = entity.Name
        };
    }

    public static Points ToEntity(MapPointDto dto)
    {
        return new Points
        {
            Id = dto.Id,
            MapId = dto.MapId,
            PathId = dto.PathId,
            Offset = dto.Offset,
            Type = dto.Type,
            Name = dto.Name
        };
    }
}
