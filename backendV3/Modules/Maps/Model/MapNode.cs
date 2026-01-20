using NetTopologySuite.Geometries;

namespace BackendV3.Modules.Maps.Model;

public sealed class MapNode
{
    public Guid NodeId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public Point Location { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool IsMaintenance { get; set; }
    public double? JunctionSpeedLimit { get; set; }
    public string? MetadataJson { get; set; }
}

