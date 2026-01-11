using System;

namespace Robot.Contracts.Dto;

public class RobotStateDto
{
    public string RobotId { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string RuntimeMode { get; set; } = "";
    public string Mode { get; set; } = "";
    public string MotionState { get; set; } = "";
    public double CurrentLinearVel { get; set; }
    public double TargetLinearVel { get; set; }
    public string CamSide { get; set; } = "";
    public string SafetyStopReason { get; set; } = "";
    public double? BatteryPct { get; set; }
    public string? LastQrCode { get; set; }
    public object? TrackPosition { get; set; }
    public bool TeachEnabled { get; set; }
    public string? TeachSessionId { get; set; }
    public string? TeachStepId { get; set; }
    public RobotActuatorsDto Actuators { get; set; } = new();
    public RobotCapabilitiesDto? Capabilities { get; set; }
    public RobotFeatureFlagsDto? FeatureFlags { get; set; }
    public string? Ip { get; set; }
    public string? Name { get; set; }
}

