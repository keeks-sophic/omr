namespace backend.DTOs;

public class RouteRequestDto
{
    public string RobotIp { get; set; } = string.Empty;
    public int MapId { get; set; }
    public int? DestinationNodeId { get; set; }
    public double? DestX { get; set; }
    public double? DestY { get; set; }
    public double? StartX { get; set; }
    public double? StartY { get; set; }
    public string Mode { get; set; } = "dispatch";
}
