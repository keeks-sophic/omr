using System;

namespace Robot.Contracts.Routes;

public class RouteProgress
{
    public string RobotId { get; set; } = "";
    public string RouteId { get; set; } = "";
    public int SegmentIndex { get; set; }
    public double DistanceAlong { get; set; }
    public DateTimeOffset? Eta { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
