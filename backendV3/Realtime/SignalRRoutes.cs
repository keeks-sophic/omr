namespace BackendV3.Realtime;

public static class SignalRRoutes
{
    public const string RealtimeHubPath = "/hubs/realtime";

    public static class Events
    {
        public const string MapVersionCreated = "map.version.created";
        public const string MapVersionPublished = "map.version.published";
        public const string MapEntityUpdated = "map.entity.updated";

        public const string RobotIdentityUpdated = "robot.identity.updated";
        public const string RobotCapabilityUpdated = "robot.capability.updated";
        public const string RobotStatusUpdated = "robot.status.updated";
        public const string RobotTelemetryUpdated = "robot.telemetry.updated";
        public const string RobotSettingsReportedUpdated = "robot.settings.reported.updated";
        public const string RobotMetaUpdated = "robot.meta.updated";
        public const string RobotCommandAck = "robot.command.ack";
    }
}
