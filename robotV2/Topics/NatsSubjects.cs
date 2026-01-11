namespace Robot.Topics;

public static class NatsSubjects
{
    public static string Robot(string robotId, string suffix) => $"robot.{robotId}.{suffix}";
    public static class Cmd
    {
        public static string Grip(string robotId) => Robot(robotId, "cmd.grip");
        public static string Hoist(string robotId) => Robot(robotId, "cmd.hoist");
        public static string Telescope(string robotId) => Robot(robotId, "cmd.telescope");
        public static string CamToggle(string robotId) => Robot(robotId, "cmd.cam_toggle");
        public static string Rotate(string robotId) => Robot(robotId, "cmd.rotate");
        public static string Mode(string robotId) => Robot(robotId, "cmd.mode");
        public static string Ack(string robotId) => Robot(robotId, "cmd_ack");
    }
    public static class Task
    {
        public static string Assign(string robotId) => Robot(robotId, "task.assign");
        public static string Control(string robotId) => Robot(robotId, "task.control");
        public static string Event(string robotId) => Robot(robotId, "task.event");
    }
    public static class Route
    {
        public static string Assign(string robotId) => Robot(robotId, "route.assign");
        public static string Update(string robotId) => Robot(robotId, "route.update");
        public static string Progress(string robotId) => Robot(robotId, "route.progress");
    }
    public static class Cfg
    {
        public static string MotionLimits(string robotId) => Robot(robotId, "cfg.motion_limits");
        public static string RuntimeMode(string robotId) => Robot(robotId, "cfg.runtime_mode");
        public static string Features(string robotId) => Robot(robotId, "cfg.features");
    }
    public static class Traffic
    {
        public static string Schedule(string robotId) => Robot(robotId, "traffic.schedule");
    }
    public static class Presence
    {
        public static string Hello(string robotId) => Robot(robotId, "presence.hello");
        public static string Heartbeat(string robotId) => Robot(robotId, "presence.heartbeat");
    }
    public static class State
    {
        public static string Snapshot(string robotId) => Robot(robotId, "state.snapshot");
        public static string Event(string robotId) => Robot(robotId, "state.event");
    }
    public static class Telemetry
    {
        public static string Battery(string robotId) => Robot(robotId, "telemetry.battery");
        public static string Health(string robotId) => Robot(robotId, "telemetry.health");
        public static string Pose(string robotId) => Robot(robotId, "telemetry.pose");
        public static string Motion(string robotId) => Robot(robotId, "telemetry.motion");
        public static string Radar(string robotId) => Robot(robotId, "telemetry.radar");
        public static string Qr(string robotId) => Robot(robotId, "telemetry.qr");
    }
    public static class Log
    {
        public static string Event(string robotId) => Robot(robotId, "log.event");
    }
    public static class Rpc
    {
        public static string Ping(string robotId) => Robot(robotId, "rpc.ping");
        public static string GetState(string robotId) => Robot(robotId, "rpc.get_state");
        public static string GetVersions(string robotId) => Robot(robotId, "rpc.get_versions");
    }
    public static string[] BackendToRobotSubjects(string robotId) => new[]
    {
        Cmd.Grip(robotId),
        Cmd.Hoist(robotId),
        Cmd.Telescope(robotId),
        Cmd.CamToggle(robotId),
        Cmd.Rotate(robotId),
        Cmd.Mode(robotId),
        Task.Assign(robotId),
        Task.Control(robotId),
        Route.Assign(robotId),
        Route.Update(robotId),
        Cfg.MotionLimits(robotId),
        Cfg.RuntimeMode(robotId),
        Cfg.Features(robotId),
        Traffic.Schedule(robotId),
        Rpc.Ping(robotId),
        Rpc.GetState(robotId),
        Rpc.GetVersions(robotId)
    };
    public static string[] RobotToBackendSubjects(string robotId) => new[]
    {
        Presence.Hello(robotId),
        Presence.Heartbeat(robotId),
        Cmd.Ack(robotId),
        State.Snapshot(robotId),
        State.Event(robotId),
        Task.Event(robotId),
        Route.Progress(robotId),
        Telemetry.Battery(robotId),
        Telemetry.Health(robotId),
        Telemetry.Pose(robotId),
        Telemetry.Motion(robotId),
        Telemetry.Radar(robotId),
        Telemetry.Qr(robotId),
        Log.Event(robotId)
    };
}
