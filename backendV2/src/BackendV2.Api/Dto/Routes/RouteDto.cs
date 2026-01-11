using System;
using System.Collections.Generic;

namespace BackendV2.Api.Dto.Routes;

public class RouteDto
{
    public Guid RouteId { get; set; }
    public Guid MapVersionId { get; set; }
    public PointDto Start { get; set; } = new PointDto();
    public PointDto Goal { get; set; } = new PointDto();
    public List<RouteSegmentDto> Segments { get; set; } = new List<RouteSegmentDto>();
    public DateTimeOffset? EstimatedStartTime { get; set; }
    public DateTimeOffset? EstimatedArrivalTime { get; set; }
}
