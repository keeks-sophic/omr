using Robot.Services;
using Robot.Topics;
using Robot.Domain.State;
using Robot.Contracts.Dto;

namespace Robot.Workers;

public class StateSnapshotWorker
{
    private readonly NatsService _nats;
    private readonly RobotStateStore _store;
    public StateSnapshotWorker(NatsService nats, RobotStateStore store)
    {
        _nats = nats;
        _store = store;
    }
    public void PublishSnapshot(string robotId)
    {
        var subject = NatsSubjects.State.Snapshot(robotId);
        var dto = RobotStateMapper.ToDto(_store.State);
        _nats.Publish(subject);
    }
    public void PublishEvent(string robotId, string[] changedFields)
    {
        var subject = NatsSubjects.State.Event(robotId);
        var evt = new RobotStateEvent
        {
            RobotId = robotId,
            Timestamp = System.DateTimeOffset.UtcNow,
            ChangedFields = changedFields,
            State = RobotStateMapper.ToDto(_store.State)
        };
        _nats.Publish(subject);
    }
}
