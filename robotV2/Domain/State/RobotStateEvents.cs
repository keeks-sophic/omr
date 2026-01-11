using System;

namespace Robot.Domain.State;

public abstract class RobotEvent
{
    public DateTimeOffset Timestamp { get; set; }
    public string Source { get; set; } = "Internal";
}

public class IdentityResolved : RobotEvent
{
    public string RobotId { get; set; } = "";
    public string? Name { get; set; }
    public string? Ip { get; set; }
}

public class MotionLimitsUpdated : RobotEvent
{
    public Motion.MotionLimits Limits { get; set; } = new();
}

public class RuntimeModeUpdated : RobotEvent
{
    public string RuntimeMode { get; set; } = "LIVE";
}

public class ModeUpdated : RobotEvent
{
    public string Mode { get; set; } = "IDLE";
    public bool TeachEnabled { get; set; }
    public string? TeachSessionId { get; set; }
}

public class FeatureFlagsUpdated : RobotEvent
{
    public Config.FeatureFlags Flags { get; set; } = new();
}

public class CommandAccepted : RobotEvent
{
    public string CorrelationId { get; set; } = "";
    public string CommandType { get; set; } = "";
}

public class CommandRejected : RobotEvent
{
    public string CorrelationId { get; set; } = "";
    public string Reason { get; set; } = "";
}

public class ActuatorStateChanged : RobotEvent
{
    public RobotActuators Actuators { get; set; } = new();
}

public class TaskAssigned : RobotEvent
{
    public string TaskId { get; set; } = "";
    public string TaskType { get; set; } = "";
    public object? Parameters { get; set; }
}

public class TaskStatusChanged : RobotEvent
{
    public string TaskId { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
}

public class RouteAssigned : RobotEvent
{
    public string RouteId { get; set; } = "";
    public TaskRoute.RouteSegment[] Segments { get; set; } = System.Array.Empty<TaskRoute.RouteSegment>();
}

public class RouteProgressUpdated : RobotEvent
{
    public TaskRoute.RouteProgress Progress { get; set; } = new();
}

public class RadarObstacleChanged : RobotEvent
{
    public bool ObstacleDetected { get; set; }
}

public class EStopChanged : RobotEvent
{
    public bool EstopActive { get; set; }
}

public class FaultRaised : RobotEvent
{
    public string Reason { get; set; } = "";
}

public class FaultCleared : RobotEvent
{
}

public class MotionTargetUpdated : RobotEvent
{
    public double TargetLinearVel { get; set; }
}

public class MotionCurrentUpdated : RobotEvent
{
    public double CurrentLinearVel { get; set; }
}

public class QrScanned : RobotEvent
{
    public string Code { get; set; } = "";
}
