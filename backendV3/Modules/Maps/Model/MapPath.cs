using NetTopologySuite.Geometries;

namespace BackendV3.Modules.Maps.Model;

public sealed class MapPath
{
    public Guid PathId { get; set; }
    public Guid MapVersionId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string Direction { get; set; } = "TWO_WAY";
    public LineString Location { get; set; } = default!;
    public double LengthMeters { get; set; }
    public bool IsActive { get; set; }
    public bool IsMaintenance { get; set; }
    public double? SpeedLimit { get; set; }
    public bool IsRestPath { get; set; }
    public int? RestCapacity { get; set; }
    public string? RestDwellPolicy { get; set; }
    public string? MetadataJson { get; set; }
}

