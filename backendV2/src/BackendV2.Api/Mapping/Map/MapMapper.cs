using BackendV2.Api.Dto.Map;
using BackendV2.Api.Model.Map;

namespace BackendV2.Api.Mapping.Map;

public static class MapMapper
{
    public static MapVersionDto ToDto(MapVersion v)
    {
        return new MapVersionDto
        {
            MapVersionId = v.MapVersionId,
            Name = v.Name,
            Version = v.Version,
            IsActive = v.IsActive,
            CreatedBy = v.CreatedBy ?? default,
            CreatedAt = v.CreatedAt,
            PublishedAt = v.PublishedAt,
            ChangeSummary = v.ChangeSummary
        };
    }

    public static NodeDto ToDto(MapNode n)
    {
        return new NodeDto
        {
            NodeId = n.NodeId,
            MapVersionId = n.MapVersionId,
            Name = n.Name,
            X = n.Location.X,
            Y = n.Location.Y,
            IsActive = n.IsActive,
            IsMaintenance = n.IsMaintenance,
            Metadata = string.IsNullOrWhiteSpace(n.MetadataJson) ? null : n.MetadataJson
        };
    }

    public static QrAnchorDto ToDto(QrAnchor q)
    {
        return new QrAnchorDto
        {
            QrId = q.QrId,
            MapVersionId = q.MapVersionId,
            QrCode = q.QrCode,
            X = q.Location.X,
            Y = q.Location.Y,
            PathId = q.PathId,
            DistanceAlongPath = q.DistanceAlongPath
        };
    }

    public static MapPointDto ToDto(MapPoint p)
    {
        return new MapPointDto
        {
            PointId = p.PointId,
            MapVersionId = p.MapVersionId,
            Name = p.Name,
            Type = p.Type,
            X = p.Location.X,
            Y = p.Location.Y,
            AttachedNodeId = p.AttachedNodeId
        };
    }
}
