namespace Robot.Topics;

public static class NatsSubjects
{
    public const string TelemetryPrefix = "robots.telemetry";
    public const string CommandPrefix = "robots.cmd";
    public const string RoutePlanPrefix = "robots.route.plan";
    public const string RouteSegmentPrefix = "robots.route.segment";
    public const string ControlPrefix = "robots.control";
    public const string SyncPrefix = "robots.sync";
}

