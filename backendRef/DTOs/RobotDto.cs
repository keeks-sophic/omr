namespace backend.DTOs;

public class RobotDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string State { get; set; } = "idle";
    public double Battery { get; set; }
    
    public bool Connected { get; set; }
    public DateTime? LastActive { get; set; }
}
