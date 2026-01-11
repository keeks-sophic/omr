using Robot.Contracts.Dto;

namespace Robot.Domain.State;

public static class RobotStateMapper
{
    public static RobotStateDto ToDto(RobotState s)
    {
        return new RobotStateDto
        {
            RobotId = s.Identity.Id,
            Timestamp = System.DateTimeOffset.UtcNow,
            RuntimeMode = s.RuntimeMode,
            Mode = s.Mode,
            MotionState = s.Motion.MotionStateName,
            CurrentLinearVel = s.Motion.CurrentLinearVel,
            TargetLinearVel = s.Motion.TargetLinearVel,
            CamSide = s.Motion.CamSide,
            SafetyStopReason = s.Safety.SafetyStopReason,
            BatteryPct = s.Health.BatteryPct,
            LastQrCode = s.Localization.LastQrCode,
            TrackPosition = s.Localization.TrackPosition,
            TeachEnabled = s.TeachEnabled,
            TeachSessionId = s.TeachSessionId,
            TeachStepId = s.TeachStepId,
            Actuators = new RobotActuatorsDto
            {
                HoistPosition = s.Actuators.HoistPosition,
                TelescopePosition = s.Actuators.TelescopePosition,
                GripState = s.Actuators.GripState,
                RotatePosition = s.Actuators.RotatePosition
            },
            Capabilities = new RobotCapabilitiesDto
            {
                SupportsCamToggle = s.Capabilities.SupportsCamToggle,
                SupportsRadar = s.Capabilities.SupportsRadar,
                SupportsQrReader = s.Capabilities.SupportsQrReader,
                SupportsHoist = s.Capabilities.SupportsHoist,
                SupportsTelescope = s.Capabilities.SupportsTelescope,
                SupportsGrip = s.Capabilities.SupportsGrip,
                SupportsRotate = s.Capabilities.SupportsRotate
            },
            FeatureFlags = new RobotFeatureFlagsDto
            {
                TelescopeEnabled = s.FeatureFlags.TelescopeEnabled
            },
            Ip = s.Identity.Ip,
            Name = s.Identity.Name
        };
    }
}
