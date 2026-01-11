using System.Collections.Generic;
using BackendV2.Api.Dto.Routes;

namespace BackendV2.Api.Dto.Routes;

public class RouteSegmentDto
{
    public System.Guid PathId { get; set; }
    public string Direction { get; set; } = "FORWARD";
    public List<PointDto>? Checkpoints { get; set; }
    public double? EstimatedSeconds { get; set; }
}
