using Robot.Topics;

namespace Robot.Services;

public class TelemetryService
{
    private readonly NatsService _nats;
    public TelemetryService(NatsService nats)
    {
        _nats = nats;
    }
    public void RegisterPublishers(string robotId)
    {
        _nats.Publish(NatsSubjects.Presence.Hello(robotId));
        _nats.Publish(NatsSubjects.Presence.Heartbeat(robotId));
        _nats.Publish(NatsSubjects.Cmd.Ack(robotId));
        _nats.Publish(NatsSubjects.State.Snapshot(robotId));
        _nats.Publish(NatsSubjects.State.Event(robotId));
        _nats.Publish(NatsSubjects.Task.Event(robotId));
        _nats.Publish(NatsSubjects.Route.Progress(robotId));
        _nats.Publish(NatsSubjects.Telemetry.Battery(robotId));
        _nats.Publish(NatsSubjects.Telemetry.Health(robotId));
        _nats.Publish(NatsSubjects.Telemetry.Pose(robotId));
        _nats.Publish(NatsSubjects.Telemetry.Motion(robotId));
        _nats.Publish(NatsSubjects.Telemetry.Radar(robotId));
        _nats.Publish(NatsSubjects.Telemetry.Qr(robotId));
        _nats.Publish(NatsSubjects.Log.Event(robotId));
    }
    public void SendAck(string robotId, string correlationId, string status, string? reason = null)
    {
        var subject = NatsSubjects.Cmd.Ack(robotId);
        _nats.Publish(subject);
    }
    public void PublishTaskEvent(string robotId, string taskId, string status, string? reason = null)
    {
        var subject = NatsSubjects.Task.Event(robotId);
        _nats.Publish(subject);
    }
    public void PublishRouteProgress(string robotId)
    {
        var subject = NatsSubjects.Route.Progress(robotId);
        _nats.Publish(subject);
    }
    public void PublishLogEvent(string robotId, string level, string message)
    {
        var subject = NatsSubjects.Log.Event(robotId);
        _nats.Publish(subject);
    }
    public void PublishRadar(string robotId)
    {
        var subject = NatsSubjects.Telemetry.Radar(robotId);
        _nats.Publish(subject);
    }
    public void PublishQr(string robotId)
    {
        var subject = NatsSubjects.Telemetry.Qr(robotId);
        _nats.Publish(subject);
    }
    public void PublishMotion(string robotId)
    {
        var subject = NatsSubjects.Telemetry.Motion(robotId);
        _nats.Publish(subject);
    }
    public void PublishPose(string robotId)
    {
        var subject = NatsSubjects.Telemetry.Pose(robotId);
        _nats.Publish(subject);
    }
    public void PublishBattery(string robotId)
    {
        var subject = NatsSubjects.Telemetry.Battery(robotId);
        _nats.Publish(subject);
    }
    public void PublishHealth(string robotId)
    {
        var subject = NatsSubjects.Telemetry.Health(robotId);
        _nats.Publish(subject);
    }
}
