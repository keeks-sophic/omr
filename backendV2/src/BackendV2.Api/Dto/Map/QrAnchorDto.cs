using System;

namespace BackendV2.Api.Dto.Map;

public class QrAnchorDto
{
    public Guid QrId { get; set; }
    public Guid MapVersionId { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public Guid PathId { get; set; }
    public double DistanceAlongPath { get; set; }
}
