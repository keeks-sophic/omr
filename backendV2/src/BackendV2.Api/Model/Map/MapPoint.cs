using System;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Model.Map;

public class MapPoint
{
    public Guid PointId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Point Location { get; set; } = default!;
    public Guid? AttachedNodeId { get; set; }
    public string? MetadataJson { get; set; }
}
