using Robot.Contracts.Dto;

namespace Robot.Domain.State;

public class RobotStateStore
{
    public RobotState State { get; } = new();
    public string[] Apply(RobotEvent ev)
    {
        var changed = new System.Collections.Generic.List<string>();
        switch (ev)
        {
            case IdentityResolved id:
                State.Identity = new Identity.RobotIdentity(id.RobotId, id.Name, id.Ip);
                changed.Add("identity");
                break;
            case MotionLimitsUpdated ml:
                State.MotionLimits.MaxDriveSpeed = ml.Limits.MaxDriveSpeed;
                State.MotionLimits.MaxAcceleration = ml.Limits.MaxAcceleration;
                State.MotionLimits.MaxDeceleration = ml.Limits.MaxDeceleration;
                changed.Add("motionLimits");
                break;
            case RuntimeModeUpdated rm:
                State.RuntimeMode = rm.RuntimeMode;
                changed.Add("runtimeMode");
                break;
            case FeatureFlagsUpdated ff:
                State.FeatureFlags.TelescopeEnabled = ff.Flags.TelescopeEnabled;
                changed.Add("featureFlags");
                break;
            case ModeUpdated mu:
                State.Mode = mu.Mode;
                State.TeachEnabled = mu.TeachEnabled;
                State.TeachSessionId = mu.TeachSessionId;
                changed.Add("mode");
                changed.Add("teach");
                break;
            case ActuatorStateChanged ac:
                State.Actuators.HoistPosition = ac.Actuators.HoistPosition;
                State.Actuators.TelescopePosition = ac.Actuators.TelescopePosition;
                State.Actuators.GripState = ac.Actuators.GripState;
                State.Actuators.RotatePosition = ac.Actuators.RotatePosition;
                changed.Add("actuators");
                break;
            case TaskAssigned ta:
                State.ActiveTask = new TaskRoute.ActiveTask { TaskId = ta.TaskId, TaskType = ta.TaskType, Parameters = ta.Parameters };
                changed.Add("activeTask");
                break;
            case RouteAssigned ra:
                State.ActiveRoute = new TaskRoute.ActiveRoute { RouteId = ra.RouteId, Segments = ra.Segments };
                changed.Add("activeRoute");
                break;
            case RouteProgressUpdated rp:
                State.Localization.TrackPosition = rp.Progress;
                changed.Add("routeProgress");
                break;
            case RadarObstacleChanged ro:
                State.Safety.ObstacleDetected = ro.ObstacleDetected;
                changed.Add("safety");
                break;
            case EStopChanged es:
                State.Safety.EstopActive = es.EstopActive;
                changed.Add("safety");
                break;
            case FaultRaised fr:
                State.Health.LastError = fr.Reason;
                changed.Add("health");
                break;
            case FaultCleared:
                State.Health.LastError = null;
                changed.Add("health");
                break;
            case MotionTargetUpdated mt:
                State.Motion.TargetLinearVel = mt.TargetLinearVel;
                var prevState = State.Motion.MotionStateName;
                State.Motion.MotionStateName = mt.TargetLinearVel > 0 ? "MOVING" : "STOPPED";
                changed.Add("motionTarget");
                if (State.Motion.MotionStateName != prevState) changed.Add("motionState");
                break;
            case MotionCurrentUpdated mc:
                State.Motion.CurrentLinearVel = mc.CurrentLinearVel;
                var prev = State.Motion.MotionStateName;
                State.Motion.MotionStateName = mc.CurrentLinearVel > 0 ? "MOVING" : "STOPPED";
                changed.Add("motionCurrent");
                if (State.Motion.MotionStateName != prev) changed.Add("motionState");
                break;
            case QrScanned qs:
                State.Localization.LastQrCode = qs.Code;
                State.Localization.LastQrTime = qs.Timestamp;
                changed.Add("qr");
                break;
        }
        return changed.ToArray();
    }
}
