using System;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Model.Map;

public class MapPath
{
    public Guid PathId { get; set; }
    public Guid MapVersionId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public LineString Location { get; set; } = default!;
    public bool TwoWay { get; set; }
    public bool IsActive { get; set; }
    public bool IsMaintenance { get; set; }
    public double? SpeedLimit { get; set; }
    public bool IsRestPath { get; set; }
    public int? RestCapacity { get; set; }
    public string? RestDwellPolicyJson { get; set; }
    public double? MinFollowingDistanceMeters { get; set; }
    public string? MetadataJson { get; set; }
}
