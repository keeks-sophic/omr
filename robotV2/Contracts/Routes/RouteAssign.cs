namespace Robot.Contracts.Routes;

public class RouteAssign
{
    public string CorrelationId { get; set; } = "";
    public string RouteId { get; set; } = "";
    public string MapVersionId { get; set; } = "";
    public RouteSegment[] Segments { get; set; } = System.Array.Empty<RouteSegment>();
}
