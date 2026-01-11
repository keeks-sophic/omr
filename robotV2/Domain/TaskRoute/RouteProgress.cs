using System;

namespace Robot.Domain.TaskRoute;

public class RouteProgress
{
    public string RouteId { get; set; } = "";
    public int SegmentIndex { get; set; }
    public double DistanceAlong { get; set; }
    public DateTimeOffset? Eta { get; set; }
}

