using System;

namespace BackendV2.Api.Dto.Core;

public class RobotDto
{
    public string RobotId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Ip { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string? State { get; set; }
    public double? Battery { get; set; }
    public bool Connected { get; set; }
    public DateTimeOffset? LastActive { get; set; }
    public Guid? MapVersionId { get; set; }
}
