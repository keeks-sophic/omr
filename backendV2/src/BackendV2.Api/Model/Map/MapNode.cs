using System;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Model.Map;

public class MapNode
{
    public Guid NodeId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Point Location { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool IsMaintenance { get; set; }
    public string? MetadataJson { get; set; }
}
