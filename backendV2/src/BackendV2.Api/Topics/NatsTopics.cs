namespace BackendV2.Api.Topics;

public static class NatsTopics
{
    public static string RobotCmd(string robotId, string action) => $"robot.{robotId}.cmd.{action}";
    public static string RobotCmdGrip(string robotId) => $"robot.{robotId}.cmd.grip";
    public static string RobotCmdHoist(string robotId) => $"robot.{robotId}.cmd.hoist";
    public static string RobotCmdTelescope(string robotId) => $"robot.{robotId}.cmd.telescope";
    public static string RobotCmdCamToggle(string robotId) => $"robot.{robotId}.cmd.cam_toggle";
    public static string RobotCmdRotate(string robotId) => $"robot.{robotId}.cmd.rotate";
    public static string RobotCmdMode(string robotId) => $"robot.{robotId}.cmd.mode";
    public static string RobotTaskAssign(string robotId) => $"robot.{robotId}.task.assign";
    public static string RobotTaskControl(string robotId) => $"robot.{robotId}.task.control";
    public static string RobotRouteAssign(string robotId) => $"robot.{robotId}.route.assign";
    public static string RobotRouteUpdate(string robotId) => $"robot.{robotId}.route.update";
    public static string RobotCfgMotionLimits(string robotId) => $"robot.{robotId}.cfg.motion_limits";
    public static string RobotCfgRuntimeMode(string robotId) => $"robot.{robotId}.cfg.runtime_mode";
    public static string RobotCfgFeatures(string robotId) => $"robot.{robotId}.cfg.features";
    public static string RobotTrafficSchedule(string robotId) => $"robot.{robotId}.traffic.schedule";
    public static string RobotCmdAck(string robotId) => $"robot.{robotId}.cmd_ack";
    public static string RobotPresenceHello(string robotId) => $"robot.{robotId}.presence.hello";
    public static string RobotPresenceHeartbeat(string robotId) => $"robot.{robotId}.presence.heartbeat";
    public static string RobotStateSnapshot(string robotId) => $"robot.{robotId}.state.snapshot";
    public static string RobotStateEvent(string robotId) => $"robot.{robotId}.state.event";
    public static string RobotTaskEvent(string robotId) => $"robot.{robotId}.task.event";
    public static string RobotRouteProgress(string robotId) => $"robot.{robotId}.route.progress";
    public static string RobotTelemetryBattery(string robotId) => $"robot.{robotId}.telemetry.battery";
    public static string RobotTelemetryHealth(string robotId) => $"robot.{robotId}.telemetry.health";
    public static string RobotTelemetryPose(string robotId) => $"robot.{robotId}.telemetry.pose";
    public static string RobotTelemetryMotion(string robotId) => $"robot.{robotId}.telemetry.motion";
    public static string RobotTelemetryRadar(string robotId) => $"robot.{robotId}.telemetry.radar";
    public static string RobotTelemetryQr(string robotId) => $"robot.{robotId}.telemetry.qr";
    public static string RobotLogEvent(string robotId) => $"robot.{robotId}.log.event";
    public static string RobotRpcPing(string robotId) => $"robot.{robotId}.rpc.ping";
    public static string RobotRpcGetState(string robotId) => $"robot.{robotId}.rpc.get_state";
    public static string RobotRpcGetVersions(string robotId) => $"robot.{robotId}.rpc.get_versions";
    public static string BackendReplayStart() => "backend.replay.start";
    public static string BackendReplayStop() => "backend.replay.stop";
    public static string BackendReplayEvent(string replaySessionId) => $"backend.replay.{replaySessionId}.event";
    public static string BackendReplayStatus(string replaySessionId) => $"backend.replay.{replaySessionId}.status";
    public static string BackendSimStart() => "backend.sim.start";
    public static string BackendSimStop() => "backend.sim.stop";
    public static string BackendSimStatus(string simSessionId) => $"backend.sim.{simSessionId}.status";
}
