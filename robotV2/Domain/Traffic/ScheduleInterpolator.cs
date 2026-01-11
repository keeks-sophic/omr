using System;

namespace Robot.Domain.Traffic;

public class ScheduleInterpolator
{
    public double InterpolateTargetVel(Contracts.Traffic.TrafficSchedule schedule, DateTimeOffset now)
    {
        if (schedule.Points == null || schedule.Points.Length == 0) return 0;
        var tMs = (int)Math.Max(0, (now - schedule.GeneratedAt).TotalMilliseconds);
        // Find surrounding points
        Contracts.Traffic.SchedulePoint? prev = null;
        Contracts.Traffic.SchedulePoint? next = null;
        foreach (var p in schedule.Points)
        {
            if (p.TMs <= tMs) prev = p;
            if (p.TMs >= tMs)
            {
                next = p;
                break;
            }
        }
        if (prev == null) prev = schedule.Points[0];
        if (next == null) next = schedule.Points[schedule.Points.Length - 1];
        if (prev == next) return prev.TargetVel;
        var dt = next.TMs - prev.TMs;
        if (dt <= 0) return next.TargetVel;
        var alpha = (double)(tMs - prev.TMs) / dt;
        return prev.TargetVel + alpha * (next.TargetVel - prev.TargetVel);
    }
}
