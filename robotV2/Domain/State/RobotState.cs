using System;
using Robot.Domain.Config;
using Robot.Domain.Identity;
using Robot.Domain.Motion;
using Robot.Domain.TaskRoute;

namespace Robot.Domain.State;

public class RobotState
{
    public RobotIdentity Identity { get; set; } = new("unknown", null, null);
    public string RuntimeMode { get; set; } = "LIVE";
    public string Mode { get; set; } = "IDLE";
    public bool TeachEnabled { get; set; }
    public string? TeachSessionId { get; set; }
    public string? TeachStepId { get; set; }
    public MotionLimits MotionLimits { get; set; } = new();
    public FeatureFlags FeatureFlags { get; set; } = new();
    public RobotCapabilities Capabilities { get; set; } = new();
    public MotionState Motion { get; set; } = new();
    public RobotActuators Actuators { get; set; } = new();
    public SafetyState Safety { get; set; } = new();
    public HealthState Health { get; set; } = new();
    public TrackLocalization Localization { get; set; } = new();
    public ActiveTask? ActiveTask { get; set; }
    public ActiveRoute? ActiveRoute { get; set; }
    public DateTimeOffset? LastBackendSeen { get; set; }
}
