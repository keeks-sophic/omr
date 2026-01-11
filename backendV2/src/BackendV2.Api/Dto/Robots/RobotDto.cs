using System;

namespace BackendV2.Api.Dto.Robots;

public class RobotDto
{
    public string RobotId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public Guid? MapVersionId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string State { get; set; } = string.Empty;
    public double Battery { get; set; }
    public bool Connected { get; set; }
    public DateTimeOffset LastActive { get; set; }
}
