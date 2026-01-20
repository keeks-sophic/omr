using NetTopologySuite.Geometries;

namespace BackendV3.Modules.Maps.Model;

public sealed class MapPoint
{
    public Guid PointId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Type { get; set; } = "PICK_DROP";
    public string Label { get; set; } = string.Empty;
    public Point Location { get; set; } = default!;
    public Guid? AttachedNodeId { get; set; }
    public string? MetadataJson { get; set; }
}

