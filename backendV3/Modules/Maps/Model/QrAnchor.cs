using NetTopologySuite.Geometries;

namespace BackendV3.Modules.Maps.Model;

public sealed class QrAnchor
{
    public Guid QrId { get; set; }
    public Guid MapVersionId { get; set; }
    public Guid PathId { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public double DistanceAlongPath { get; set; }
    public Point? Location { get; set; }
    public string? MetadataJson { get; set; }
}

