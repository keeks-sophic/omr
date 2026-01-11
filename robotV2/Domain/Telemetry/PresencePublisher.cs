using Robot.Services;
using Robot.Contracts.Presence;
using Robot.Topics;

namespace Robot.Domain.Telemetry;

public class PresencePublisher
{
    private readonly NatsService _nats;
    public PresencePublisher(NatsService nats)
    {
        _nats = nats;
    }
    public void PublishHello(string robotId)
    {
        var subject = NatsSubjects.Presence.Hello(robotId);
        var hello = new PresenceHello
        {
            RobotId = robotId,
            SoftwareVersion = "0.0.1",
            RuntimeMode = "LIVE",
            Capabilities = new Contracts.Dto.RobotCapabilitiesDto(),
            FeatureFlags = new Contracts.Dto.RobotFeatureFlagsDto()
        };
        _nats.Publish(subject);
    }
}
