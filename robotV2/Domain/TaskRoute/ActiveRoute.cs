namespace Robot.Domain.TaskRoute;

public class ActiveRoute
{
    public string RouteId { get; set; } = "";
    public RouteSegment[] Segments { get; set; } = System.Array.Empty<RouteSegment>();
}

public class RouteSegment
{
    public string PathId { get; set; } = "";
    public string Direction { get; set; } = "FORWARD";
    public Point2D[]? Checkpoints { get; set; }
}

public class Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
}

