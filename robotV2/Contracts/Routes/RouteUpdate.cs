namespace Robot.Contracts.Routes;

public class RouteUpdate
{
    public string CorrelationId { get; set; } = "";
    public string RouteId { get; set; } = "";
    public RouteSegment[] Segments { get; set; } = System.Array.Empty<RouteSegment>();
}
