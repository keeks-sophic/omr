using System;

namespace Robot.Contracts.Traffic;

public class TrafficSchedule
{
    public string ScheduleId { get; set; } = "";
    public DateTimeOffset GeneratedAt { get; set; }
    public int HorizonMs { get; set; }
    public SchedulePoint[] Points { get; set; } = System.Array.Empty<SchedulePoint>();
}

public class SchedulePoint
{
    public int TMs { get; set; }
    public double TargetVel { get; set; }
}
