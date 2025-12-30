namespace Backend.Topics;

public static class NatsTopics
{
    public const string TelemetrySubjectDefault = "robot.telemetry";
    public const string CommandSubjectDefault = "robot.command";
    public const string CommandNavigateRequest = "navigate.request";
    public const string CommandRoutePlan = "route.plan";
    public const string CommandRouteSegment = "route.segment";
    public const string CommandRobotSync = "robot.sync";
    public const string CommandTrafficControl = "traffic.control";
    public const string TelemetryPrefix = "robots.telemetry";
    public const string CommandPrefix = "robots.cmd";
    public const string RoutePlanPrefix = "robots.route.plan";
    public const string RouteSegmentPrefix = "robots.route.segment";
    public const string ControlPrefix = "robots.control";
    public const string SyncPrefix = "robots.sync";
}

public class NatsOptions
{
    public string Url { get; set; } = "nats://localhost:4222";
    public string TelemetrySubject { get; set; } = NatsTopics.TelemetrySubjectDefault;
    public string CommandSubject { get; set; } = NatsTopics.CommandSubjectDefault;
    public string? TelemetryStream { get; set; } = "ROBOT_TELEMETRY";
    public string? CommandStream { get; set; } = "ROBOT_COMMAND";
    public string? NavigateStream { get; set; } = "ROBOT_NAVIGATE";
    public string? RoutePlanStream { get; set; } = "ROBOT_ROUTEPLAN";
    public string? RouteSegmentStream { get; set; } = "ROBOT_ROUTESEGMENT";
    public string? RobotSyncStream { get; set; } = "ROBOT_SYNC";
    public string? TrafficControlStream { get; set; } = "ROBOT_TRAFFIC";
    public string? TelemetryWildcardStream { get; set; } = "ROBOTS_TELEMETRY";
    public string? CommandWildcardStream { get; set; } = "ROBOTS_COMMAND";
    public string? RouteWildcardStream { get; set; } = "ROBOTS_ROUTE";
    public string? ControlWildcardStream { get; set; } = "ROBOTS_CONTROL";
}
