namespace BackendV3.Messaging;

public static class NatsJetStreamRoutes
{
    public static class Streams
    {
        public const string RobotsIn = "ROBOTS_IN";
        public const string RobotsOut = "ROBOTS_OUT";
    }

    public static class Consumers
    {
        public const string RobotsIdentityIngest = "robots_identity_ingest";
        public const string RobotsCapabilityIngest = "robots_capability_ingest";
        public const string RobotsStatusIngest = "robots_status_ingest";
        public const string RobotsTelemetryIngest = "robots_telemetry_ingest";
        public const string RobotsSettingsReportedIngest = "robots_settings_reported_ingest";
        public const string RobotsCommandAckIngest = "robots_command_ack_ingest";
    }

    public static class Subjects
    {
        public static string Identity(string robotId) => $"robots.identity.{robotId}";
        public const string IdentityAll = "robots.identity.*";

        public static string Capability(string robotId) => $"robots.capability.{robotId}";
        public const string CapabilityAll = "robots.capability.*";

        public static string Status(string robotId) => $"robots.status.{robotId}";
        public const string StatusAll = "robots.status.*";

        public static string Telemetry(string robotId) => $"robots.telemetry.{robotId}";
        public const string TelemetryAll = "robots.telemetry.*";

        public static string SettingsReported(string robotId) => $"robots.settings.reported.{robotId}";
        public const string SettingsReportedAll = "robots.settings.reported.*";

        public static string SettingsDesired(string robotId) => $"robots.settings.desired.{robotId}";
        public const string SettingsDesiredAll = "robots.settings.desired.*";

        public static string Command(string robotId) => $"robots.cmd.{robotId}";
        public const string CommandAll = "robots.cmd.*";

        public static string CommandAck(string robotId) => $"robots.cmd.ack.{robotId}";
        public const string CommandAckAll = "robots.cmd.ack.*";
    }
}

