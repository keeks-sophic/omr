using Backend.Dto;
using Backend.Model;
using NetTopologySuite.Geometries;

namespace Backend.Mapping;

public static class QrMapper
{
    public static QrDto ToDto(Qrs entity)
    {
        return new QrDto
        {
            Id = entity.Id,
            MapId = entity.MapId,
            PathId = entity.PathId,
            Data = entity.Data,
            X = entity.Location != null ? entity.Location.X : 0,
            Y = entity.Location != null ? entity.Location.Y : 0,
            OffsetStart = entity.OffsetStart
        };
    }

    public static Qrs ToEntity(QrDto dto)
    {
        var entity = new Qrs
        {
            Id = dto.Id,
            MapId = dto.MapId,
            PathId = dto.PathId,
            Data = dto.Data,
            OffsetStart = dto.OffsetStart
        };
        entity.Location = new Point(dto.X, dto.Y) { SRID = 0 };
        return entity;
    }
}
