using backend.DTOs;
using backend.Models;

namespace backend.Mappers;

public static class MapMapper
{
    public static MapDto ToDto(Map m) => new MapDto { Id = m.Id, Name = m.Name };
}
