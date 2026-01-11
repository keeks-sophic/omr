using System;

namespace BackendV2.Api.Dto.Routes;

public class RoutePlanRequest
{
    public Guid MapVersionId { get; set; }
    public string? RobotId { get; set; }
    public Guid GoalPointId { get; set; }
    public double? StartX { get; set; }
    public double? StartY { get; set; }
}
