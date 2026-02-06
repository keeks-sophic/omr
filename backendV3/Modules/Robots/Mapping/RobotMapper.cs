using System.Text.Json;
using BackendV3.Modules.Robots.Dto;
using BackendV3.Modules.Robots.Model;

namespace BackendV3.Modules.Robots.Mapping;

public static class RobotMapper
{
    public static RobotDto ToDto(
        Robot robot,
        RobotIdentitySnapshot? identity,
        RobotCapabilitySnapshot? capability,
        RobotSettingsReportedSnapshot? reportedSettings)
    {
        return new RobotDto
        {
            RobotId = robot.RobotId,
            DisplayName = robot.DisplayName,
            IsEnabled = robot.IsEnabled,
            LastSeenAt = robot.LastSeenAt,
            Identity = identity == null ? null : new RobotIdentitySummaryDto
            {
                Vendor = identity.Vendor,
                Model = identity.Model,
                FirmwareVersion = identity.FirmwareVersion,
                SerialNumber = identity.SerialNumber
            },
            Capability = TryParseJson(capability?.PayloadJson),
            ReportedSettings = TryParseJson(reportedSettings?.PayloadJson)
        };
    }

    public static RobotIdentityDto ToIdentityDto(RobotIdentitySnapshot snap) =>
        new RobotIdentityDto
        {
            RobotId = snap.RobotId,
            ReceivedAt = snap.ReceivedAt,
            Payload = JsonDocument.Parse(snap.PayloadJson)
        };

    public static RobotCapabilityDto ToCapabilityDto(RobotCapabilitySnapshot snap) =>
        new RobotCapabilityDto
        {
            RobotId = snap.RobotId,
            ReceivedAt = snap.ReceivedAt,
            Payload = JsonDocument.Parse(snap.PayloadJson)
        };

    public static RobotSettingsReportedDto ToSettingsReportedDto(RobotSettingsReportedSnapshot snap) =>
        new RobotSettingsReportedDto
        {
            RobotId = snap.RobotId,
            ReceivedAt = snap.ReceivedAt,
            Payload = JsonDocument.Parse(snap.PayloadJson)
        };

    private static JsonDocument? TryParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonDocument.Parse(json);
        }
        catch
        {
            return null;
        }
    }
}

