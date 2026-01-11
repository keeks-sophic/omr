namespace Robot.Contracts.Routes;

public class RouteSegment
{
    public string PathId { get; set; } = "";
    public string Direction { get; set; } = "FORWARD";
    public Point2D[]? Checkpoints { get; set; }
}

