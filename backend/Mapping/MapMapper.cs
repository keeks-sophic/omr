using Backend.Dto;
using Backend.Model;
using System.Linq;

namespace Backend.Mapping;

public static class MapMapper
{
    public static MapDto ToDto(Maps entity)
    {
        return new MapDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Nodes = entity.Nodes?.Select(NodeMapper.ToDto).ToList(),
            Paths = entity.Paths?.Select(PathMapper.ToDto).ToList(),
            Points = entity.Points?.Select(MapPointMapper.ToDto).ToList(),
            Qrs = entity.Qrs?.Select(QrMapper.ToDto).ToList()
        };
    }

    public static Maps ToEntity(MapDto dto)
    {
        return new Maps
        {
            Id = dto.Id,
            Name = dto.Name
        };
    }
}
