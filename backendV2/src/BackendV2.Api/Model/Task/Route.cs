using System;
using NetTopologySuite.Geometries;

namespace BackendV2.Api.Model.Task;

public class Route
{
    public Guid RouteId { get; set; }
    public Guid MapVersionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Point Start { get; set; } = default!;
    public Point Goal { get; set; } = default!;
    public string SegmentsJson { get; set; } = "[]";
    public DateTimeOffset? EstimatedStartTime { get; set; }
    public DateTimeOffset? EstimatedArrivalTime { get; set; }
}
