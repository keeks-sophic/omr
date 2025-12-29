namespace backend.DTOs;

public class RoutePlanDto
{
    public int MapId { get; set; }
    public string RobotIp { get; set; } = string.Empty;
    public int StartNodeId { get; set; }
    public int DestinationNodeId { get; set; }
    public int[] NodeIds { get; set; } = Array.Empty<int>();
    public int[] PathIds { get; set; } = Array.Empty<int>();
    public double TotalLength { get; set; }
    public string Mode { get; set; } = "dispatch";
}
