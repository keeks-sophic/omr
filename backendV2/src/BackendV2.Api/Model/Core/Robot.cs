using System;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Model.Core;

public class Robot
{
    public string RobotId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Ip { get; set; }
    public Guid? MapVersionId { get; set; }
    public Point? Location { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public string? State { get; set; }
    public double? Battery { get; set; }
    public bool Connected { get; set; }
    public DateTimeOffset? LastActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
