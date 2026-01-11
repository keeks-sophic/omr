using Robot.Services;
using Robot.Domain.State;
using Robot.Domain.TaskRoute;

namespace Robot.Domain.TaskRoute;

public class TaskRouteExecutor
{
    private readonly RobotStateStore _store;
    private readonly TelemetryService _telemetry;
    private readonly RouteProgressTracker _progress;
    public TaskRouteExecutor(RobotStateStore store, TelemetryService telemetry, RouteProgressTracker progress)
    {
        _store = store;
        _telemetry = telemetry;
        _progress = progress;
    }
    public void Tick(string robotId)
    {
        if (_store.State.ActiveRoute != null)
        {
            var rp = _progress.ComputeProgress(_store.State.ActiveRoute);
            _telemetry.PublishRouteProgress(robotId);
        }
    }
    public void TaskCompleted(string robotId, string taskId)
    {
        _telemetry.PublishTaskEvent(robotId, taskId, "COMPLETED");
    }
    public void TaskFailed(string robotId, string taskId, string reason)
    {
        _telemetry.PublishTaskEvent(robotId, taskId, "FAILED", reason);
    }
}
