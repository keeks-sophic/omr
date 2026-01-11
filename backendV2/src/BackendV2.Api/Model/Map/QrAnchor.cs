using System;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Model.Map;

public class QrAnchor
{
    public Guid QrId { get; set; }
    public Guid MapVersionId { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public Point Location { get; set; } = default!;
    public Guid PathId { get; set; }
    public double DistanceAlongPath { get; set; }
    public string? MetadataJson { get; set; }
}
