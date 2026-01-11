using System;
using Robot.Contracts.Presence;
using Robot.Services;
using Robot.Topics;

namespace Robot.Workers;

public class HeartbeatWorker
{
    private readonly NatsService _nats;
    public HeartbeatWorker(NatsService nats)
    {
        _nats = nats;
    }
    public void SendHeartbeat(string robotId, long uptimeMs, string? lastError)
    {
        var subject = NatsSubjects.Presence.Heartbeat(robotId);
        _nats.Publish(subject);
    }
}
