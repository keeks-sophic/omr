using System.Collections.Generic;
using System.Text.Json;
using BackendV2.Api.Dto.Routes;
using BackendV2.Api.Dto.Tasks;
using BackendV2.Api.Model.Task;

namespace BackendV2.Api.Mapping.Tasks;

public static class TaskMapper
{
    public static TaskDto ToDto(BackendV2.Api.Model.Task.Task t)
    {
        var parameters = string.IsNullOrWhiteSpace(t.ParametersJson) ? new object() : JsonSerializer.Deserialize<object>(t.ParametersJson) ?? new object();
        return new TaskDto
        {
            TaskId = t.TaskId,
            CreatedAt = t.CreatedAt,
            CreatedBy = t.CreatedBy,
            Status = t.Status,
            AssignmentMode = t.AssignmentMode,
            RobotId = t.RobotId,
            MapVersionId = t.MapVersionId,
            TaskType = t.TaskType,
            Parameters = parameters,
            MissionId = t.MissionId,
            CurrentRouteId = t.CurrentRouteId,
            Eta = t.Eta
        };
    }

    public static RouteDto ToDto(BackendV2.Api.Model.Task.Route r)
    {
        var segments = string.IsNullOrWhiteSpace(r.SegmentsJson) ? new List<RouteSegmentDto>() : JsonSerializer.Deserialize<List<RouteSegmentDto>>(r.SegmentsJson) ?? new List<RouteSegmentDto>();
        return new RouteDto
        {
            RouteId = r.RouteId,
            MapVersionId = r.MapVersionId,
            Start = new PointDto { X = r.Start.X, Y = r.Start.Y },
            Goal = new PointDto { X = r.Goal.X, Y = r.Goal.Y },
            Segments = segments,
            EstimatedStartTime = r.EstimatedStartTime,
            EstimatedArrivalTime = r.EstimatedArrivalTime
        };
    }
}
