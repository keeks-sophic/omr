using BackendV2.Api.Dto.Robots;
using BackendV2.Api.Model.Core;

namespace BackendV2.Api.Mapping.Core;

public static class RobotMapper
{
    public static RobotDto ToDto(Robot robot)
    {
        var x = robot.Location?.X ?? robot.X ?? 0;
        var y = robot.Location?.Y ?? robot.Y ?? 0;
        return new RobotDto
        {
            RobotId = robot.RobotId,
            Name = robot.Name ?? string.Empty,
            Ip = robot.Ip ?? string.Empty,
            MapVersionId = robot.MapVersionId,
            X = x,
            Y = y,
            State = robot.State ?? string.Empty,
            Battery = robot.Battery ?? 0,
            Connected = robot.Connected,
            LastActive = robot.LastActive ?? default
        };
    }

    public static RobotSessionDto ToDto(RobotSession session)
    {
        return new RobotSessionDto
        {
            RobotId = session.RobotId,
            Connected = session.Connected,
            LastSeen = session.LastSeen,
            RuntimeMode = session.RuntimeMode,
            SoftwareVersion = session.SoftwareVersion ?? string.Empty,
            Capabilities = new RobotCapabilitiesDto(),
            FeatureFlags = new RobotFeatureFlagsDto()
        };
    }
}
