namespace backend.DTOs;

public class RobotCommandDto
{
    public string Ip { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public object? Data { get; set; }
}
