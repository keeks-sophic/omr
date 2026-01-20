using BackendV3.Modules.Maps.Dto;
using BackendV3.Modules.Maps.Model;

namespace BackendV3.Modules.Maps.Mapping;

public static class MapMappers
{
    public static MapDto ToDto(Map m, DateTimeOffset updatedAt)
    {
        return new MapDto
        {
            MapId = m.MapId,
            Name = m.Name,
            CreatedAt = m.CreatedAt,
            ArchivedAt = m.ArchivedAt,
            ActivePublishedMapVersionId = m.ActivePublishedMapVersionId,
            UpdatedAt = updatedAt
        };
    }

    public static MapVersionDto ToDto(MapVersion v)
    {
        return new MapVersionDto
        {
            MapVersionId = v.MapVersionId,
            MapId = v.MapId,
            Version = v.Version,
            Status = v.Status,
            CreatedAt = v.CreatedAt,
            PublishedAt = v.PublishedAt,
            PublishedBy = v.PublishedBy,
            ChangeSummary = v.ChangeSummary,
            DerivedFromMapVersionId = v.DerivedFromMapVersionId,
            Label = v.Label
        };
    }

    public static NodeDto ToDto(MapNode n)
    {
        return new NodeDto
        {
            NodeId = n.NodeId,
            MapVersionId = n.MapVersionId,
            Label = n.Label,
            Geom = new GeomDto { X = n.Location.X, Y = n.Location.Y },
            IsMaintenance = n.IsMaintenance,
            JunctionSpeedLimit = n.JunctionSpeedLimit
        };
    }

    public static PathDto ToDto(MapPath p)
    {
        var coords = p.Location.Coordinates;
        var pts = coords.Select(c => new GeomDto { X = c.X, Y = c.Y }).ToArray();
        return new PathDto
        {
            PathId = p.PathId,
            MapVersionId = p.MapVersionId,
            FromNodeId = p.FromNodeId,
            ToNodeId = p.ToNodeId,
            Direction = p.Direction,
            SpeedLimit = p.SpeedLimit,
            IsMaintenance = p.IsMaintenance,
            IsRestPath = p.IsRestPath,
            RestCapacity = p.RestCapacity,
            RestDwellPolicy = p.RestDwellPolicy,
            Points = pts
        };
    }

    public static MapPointDto ToDto(MapPoint p)
    {
        return new MapPointDto
        {
            PointId = p.PointId,
            MapVersionId = p.MapVersionId,
            Type = p.Type,
            Label = p.Label,
            Geom = new GeomDto { X = p.Location.X, Y = p.Location.Y },
            AttachedNodeId = p.AttachedNodeId
        };
    }

    public static QrDto ToDto(QrAnchor q)
    {
        return new QrDto
        {
            QrId = q.QrId,
            MapVersionId = q.MapVersionId,
            PathId = q.PathId,
            QrCode = q.QrCode,
            DistanceAlongPath = q.DistanceAlongPath
        };
    }
}
