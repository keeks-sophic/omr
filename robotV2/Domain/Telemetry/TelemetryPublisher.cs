using Robot.Services;

namespace Robot.Domain.Telemetry;

public class TelemetryPublisher
{
    private readonly NatsService _nats;
    public TelemetryPublisher(NatsService nats)
    {
        _nats = nats;
    }
}
