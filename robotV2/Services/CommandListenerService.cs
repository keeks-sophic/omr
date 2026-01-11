using Robot.Topics;
using Robot.Contracts.Commands;
using Robot.Contracts.Tasks;
using Robot.Contracts.Routes;
using Robot.Contracts.Dto;
using Robot.Contracts.Traffic;
using Robot.Domain.State;
using Robot.Domain.Commands;
using Robot.Domain.TaskRoute;
using Robot.Domain.Config;
using Robot.Workers;

namespace Robot.Services;

public class CommandListenerService
{
    private readonly NatsService _nats;
    private readonly TelemetryService _telemetry;
    private readonly RobotStateStore _store;
    private readonly CommandInbox _cmdInbox;
    private readonly TaskInbox _taskInbox;
    private readonly RouteInbox _routeInbox;
    private readonly ConfigInbox _cfgInbox;
    private readonly CommandValidation _validation = new();
    private readonly CommandExecutor _executor;
    private readonly Domain.Traffic.TrafficAdapter _traffic;
    private readonly StateSnapshotWorker _snapshotWorker;
    public CommandListenerService(NatsService nats, TelemetryService telemetry, RobotStateStore store, CommandInbox cmdInbox, TaskInbox taskInbox, RouteInbox routeInbox, ConfigInbox cfgInbox, Domain.Traffic.TrafficAdapter traffic, StateSnapshotWorker snapshotWorker)
    {
        _nats = nats;
        _telemetry = telemetry;
        _store = store;
        _cmdInbox = cmdInbox;
        _taskInbox = taskInbox;
        _routeInbox = routeInbox;
        _cfgInbox = cfgInbox;
        _executor = new CommandExecutor(store.State.Actuators);
        _traffic = traffic;
        _snapshotWorker = snapshotWorker;
    }
    public void RegisterAll(string robotId)
    {
        _nats.Subscribe(NatsSubjects.Cmd.Grip(robotId));
        _nats.Subscribe(NatsSubjects.Cmd.Hoist(robotId));
        _nats.Subscribe(NatsSubjects.Cmd.Telescope(robotId));
        _nats.Subscribe(NatsSubjects.Cmd.CamToggle(robotId));
        _nats.Subscribe(NatsSubjects.Cmd.Rotate(robotId));
        _nats.Subscribe(NatsSubjects.Cmd.Mode(robotId));
        _nats.Subscribe(NatsSubjects.Task.Assign(robotId));
        _nats.Subscribe(NatsSubjects.Task.Control(robotId));
        _nats.Subscribe(NatsSubjects.Route.Assign(robotId));
        _nats.Subscribe(NatsSubjects.Route.Update(robotId));
        _nats.Subscribe(NatsSubjects.Cfg.MotionLimits(robotId));
        _nats.Subscribe(NatsSubjects.Cfg.RuntimeMode(robotId));
        _nats.Subscribe(NatsSubjects.Cfg.Features(robotId));
        _nats.Subscribe(NatsSubjects.Traffic.Schedule(robotId));
        _nats.Subscribe(NatsSubjects.Rpc.Ping(robotId));
        _nats.Subscribe(NatsSubjects.Rpc.GetState(robotId));
        _nats.Subscribe(NatsSubjects.Rpc.GetVersions(robotId));
    }
    public void AcceptGrip(string robotId, GripCommand cmd)
    {
        var block = SafetyBlockReason();
        if (block != null)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", block);
            _telemetry.PublishLogEvent(robotId, "WARN", $"Grip blocked: {block}");
            return;
        }
        if (!_store.State.Capabilities.SupportsGrip)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", "Grip not supported");
            return;
        }
        _cmdInbox.Enqueue(cmd);
        _executor.SetGrip(cmd.Action);
        _telemetry.SendAck(robotId, cmd.CorrelationId, "ACK");
        _snapshotWorker.PublishEvent(robotId, new[] { "actuators" });
    }
    public void AcceptHoist(string robotId, HoistCommand cmd)
    {
        var block = SafetyBlockReason();
        if (block != null)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", block);
            _telemetry.PublishLogEvent(robotId, "WARN", $"Hoist blocked: {block}");
            return;
        }
        if (!_store.State.Capabilities.SupportsHoist)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", "Hoist not supported");
            return;
        }
        _cmdInbox.Enqueue(cmd);
        _executor.SetHoist(cmd.Value);
        _telemetry.SendAck(robotId, cmd.CorrelationId, "ACK");
        _snapshotWorker.PublishEvent(robotId, new[] { "actuators" });
    }
    public void AcceptTelescope(string robotId, TelescopeCommand cmd)
    {
        var block = SafetyBlockReason();
        if (block != null)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", block);
            _telemetry.PublishLogEvent(robotId, "WARN", $"Telescope blocked: {block}");
            return;
        }
        if (!_store.State.Capabilities.SupportsTelescope || !_store.State.FeatureFlags.TelescopeEnabled)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", "Telescope disabled");
            return;
        }
        _cmdInbox.Enqueue(cmd);
        _executor.SetTelescope(cmd.Value);
        _telemetry.SendAck(robotId, cmd.CorrelationId, "ACK");
        _snapshotWorker.PublishEvent(robotId, new[] { "actuators" });
    }
    public void AcceptCamToggle(string robotId, CamToggleCommand cmd)
    {
        var block = SafetyBlockReason();
        if (block != null)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", block);
            _telemetry.PublishLogEvent(robotId, "WARN", $"Cam toggle blocked: {block}");
            return;
        }
        if (!_store.State.Capabilities.SupportsCamToggle)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", "Cam toggle not supported");
            return;
        }
        _cmdInbox.Enqueue(cmd);
        _telemetry.SendAck(robotId, cmd.CorrelationId, "ACK");
    }
    public void AcceptRotate(string robotId, RotateCommand cmd)
    {
        var block = SafetyBlockReason();
        if (block != null)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", block);
            _telemetry.PublishLogEvent(robotId, "WARN", $"Rotate blocked: {block}");
            return;
        }
        if (!_store.State.Capabilities.SupportsRotate)
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", "Rotate not supported");
            return;
        }
        _cmdInbox.Enqueue(cmd);
        _executor.SetRotate(cmd.Value);
        _telemetry.SendAck(robotId, cmd.CorrelationId, "ACK");
        _snapshotWorker.PublishEvent(robotId, new[] { "actuators" });
    }
    public void AcceptMode(string robotId, ModeCommand cmd)
    {
        if (!_validation.ValidateMode(cmd.Mode))
        {
            _telemetry.SendAck(robotId, cmd.CorrelationId, "NAK", "Invalid mode");
            return;
        }
        _cmdInbox.Enqueue(cmd);
        var changed = _store.Apply(new ModeUpdated { Mode = cmd.Mode, TeachEnabled = cmd.TeachEnabled ?? false, TeachSessionId = cmd.TeachSessionId });
        _telemetry.SendAck(robotId, cmd.CorrelationId, "ACK");
        _snapshotWorker.PublishEvent(robotId, changed);
    }
    public void AcceptTaskAssign(string robotId, TaskAssignment msg)
    {
        _taskInbox.Enqueue(msg);
        _telemetry.SendAck(robotId, msg.CorrelationId, "ACK");
        var changed = _store.Apply(new TaskAssigned { TaskId = msg.TaskId, TaskType = msg.TaskType, Parameters = msg.Parameters });
        _snapshotWorker.PublishEvent(robotId, changed);
    }
    public void AcceptTaskControl(string robotId, TaskControl msg)
    {
        _taskInbox.Enqueue(msg);
        _telemetry.SendAck(robotId, msg.CorrelationId, "ACK");
        HandleTaskControl(robotId, msg);
    }
    public void AcceptRouteAssign(string robotId, RouteAssign msg)
    {
        _routeInbox.Enqueue(msg);
        _telemetry.SendAck(robotId, msg.CorrelationId, "ACK");
        var changed = _store.Apply(new RouteAssigned { RouteId = msg.RouteId, Segments = MapSegments(msg.Segments) });
        _snapshotWorker.PublishEvent(robotId, changed);
    }
    public void AcceptRouteUpdate(string robotId, RouteUpdate msg)
    {
        _routeInbox.Enqueue(msg);
        _telemetry.SendAck(robotId, msg.CorrelationId, "ACK");
    }
    public void AcceptCfgMotionLimits(string robotId, string correlationId, MotionLimitsDto limits)
    {
        _cfgInbox.Enqueue(limits);
        var changed = _store.Apply(new MotionLimitsUpdated { Limits = new Domain.Motion.MotionLimits { MaxDriveSpeed = limits.MaxDriveSpeed, MaxAcceleration = limits.MaxAcceleration, MaxDeceleration = limits.MaxDeceleration } });
        _telemetry.SendAck(robotId, correlationId, "ACK");
        _snapshotWorker.PublishEvent(robotId, changed);
    }
    public void AcceptCfgRuntimeMode(string robotId, string correlationId, string runtimeMode)
    {
        _cfgInbox.Enqueue(runtimeMode);
        var changed = _store.Apply(new RuntimeModeUpdated { RuntimeMode = runtimeMode });
        _telemetry.SendAck(robotId, correlationId, "ACK");
        _snapshotWorker.PublishEvent(robotId, changed);
    }
    public void AcceptCfgFeatures(string robotId, string correlationId, RobotFeatureFlagsDto flags)
    {
        _cfgInbox.Enqueue(flags);
        var changed = _store.Apply(new FeatureFlagsUpdated { Flags = new Domain.Config.FeatureFlags { TelescopeEnabled = flags.TelescopeEnabled } });
        _telemetry.SendAck(robotId, correlationId, "ACK");
        _snapshotWorker.PublishEvent(robotId, changed);
    }
    public void ApplyTrafficSchedule(TrafficSchedule schedule)
    {
        _traffic.ApplySchedule(schedule);
    }
    public void HandleTaskControl(string robotId, TaskControl control)
    {
        if (control.Action == "PAUSE")
        {
            var ch1 = _store.Apply(new ModeUpdated { Mode = "PAUSED", TeachEnabled = _store.State.TeachEnabled, TeachSessionId = _store.State.TeachSessionId });
            var ch2 = _store.Apply(new MotionTargetUpdated { TargetLinearVel = 0 });
            _snapshotWorker.PublishEvent(robotId, new[] { "mode", "motionTarget" });
        }
        else if (control.Action == "RESUME")
        {
            var ch1 = _store.Apply(new ModeUpdated { Mode = "MANUAL", TeachEnabled = _store.State.TeachEnabled, TeachSessionId = _store.State.TeachSessionId });
            _snapshotWorker.PublishEvent(robotId, new[] { "mode" });
        }
    }
    private static Robot.Domain.TaskRoute.RouteSegment[] MapSegments(Robot.Contracts.Routes.RouteSegment[] segments)
    {
        var result = new Robot.Domain.TaskRoute.RouteSegment[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            var s = segments[i];
            result[i] = new Robot.Domain.TaskRoute.RouteSegment
            {
                PathId = s.PathId,
                Direction = s.Direction,
                Checkpoints = s.Checkpoints == null ? null : MapPoints(s.Checkpoints)
            };
        }
        return result;
    }
    private static Robot.Domain.TaskRoute.Point2D[] MapPoints(Robot.Contracts.Routes.Point2D[] points)
    {
        var result = new Robot.Domain.TaskRoute.Point2D[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var p = points[i];
            result[i] = new Robot.Domain.TaskRoute.Point2D { X = p.X, Y = p.Y };
        }
        return result;
    }
    private string? SafetyBlockReason()
    {
        if (_store.State.Safety.EstopActive) return "Estop active";
        if (!string.IsNullOrEmpty(_store.State.Health.LastError)) return "Fault active";
        return null;
    }
}
