namespace backend.DTOs;

public class RouteAssignDto
{
    public string RobotIp { get; set; } = string.Empty;
    public int MapId { get; set; }
    public int[] NodeIds { get; set; } = Array.Empty<int>();
    public int[] PathIds { get; set; } = Array.Empty<int>();
}
