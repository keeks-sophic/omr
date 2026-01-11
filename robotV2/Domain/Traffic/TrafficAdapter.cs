using System;
using Robot.Contracts.Traffic;

namespace Robot.Domain.Traffic;

public class TrafficAdapter
{
    private TrafficSchedule? _latest;
    private readonly ScheduleInterpolator _interp = new();
    private DateTimeOffset? _lastScheduleAt;
    public void ApplySchedule(TrafficSchedule schedule)
    {
        _latest = schedule;
        _lastScheduleAt = DateTimeOffset.UtcNow;
    }
    public TrafficSchedule? GetLatest() => _latest;
    public double GetCurrentTargetVel(DateTimeOffset now)
    {
        if (_latest == null) return 0;
        return _interp.InterpolateTargetVel(_latest, now);
    }
    public bool IsStale(DateTimeOffset now, int staleMs)
    {
        if (_lastScheduleAt == null) return true;
        return (now - _lastScheduleAt.Value).TotalMilliseconds > staleMs;
    }
}
