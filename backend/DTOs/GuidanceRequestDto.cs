namespace backend.DTOs;

public class GuidanceRequestDto
{
    public string RobotIp { get; set; } = string.Empty;
    public int MapId { get; set; }
    public int PathId { get; set; }
    public double Offset { get; set; }
    public int NextNodeId { get; set; }
    public int Direction { get; set; } = 1;
}
