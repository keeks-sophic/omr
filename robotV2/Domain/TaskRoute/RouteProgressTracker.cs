using Robot.Domain.TaskRoute;

namespace Robot.Domain.TaskRoute;

public class RouteProgressTracker
{
    public RouteProgress ComputeProgress(ActiveRoute route)
    {
        var progress = new RouteProgress
        {
            RouteId = route.RouteId,
            SegmentIndex = 0,
            DistanceAlong = 0,
        };
        return progress;
    }
}
