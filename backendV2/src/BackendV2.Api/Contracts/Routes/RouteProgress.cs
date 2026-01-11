using System;

namespace BackendV2.Api.Contracts.Routes;

public class RouteProgress
{
    public string RouteId { get; set; } = string.Empty;
    public int SegmentIndex { get; set; }
    public double DistanceAlong { get; set; }
    public DateTimeOffset? Eta { get; set; }
}
