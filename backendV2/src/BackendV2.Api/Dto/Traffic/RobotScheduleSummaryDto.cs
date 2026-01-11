namespace BackendV2.Api.Dto.Traffic;

public class RobotScheduleSummaryDto
{
    public string RobotId { get; set; } = string.Empty;
    public string? CurrentRouteId { get; set; }
    public double TargetLinearVel { get; set; }
    public double HeadwaySeconds { get; set; }
}
