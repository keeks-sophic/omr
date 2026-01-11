using System;

namespace BackendV2.Api.Contracts.Traffic;

public class TrafficSchedule
{
    public string ScheduleId { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public int HorizonMs { get; set; }
    public SchedulePoint[] Points { get; set; } = Array.Empty<SchedulePoint>();
}

public class SchedulePoint
{
    public int TMs { get; set; }
    public double TargetVel { get; set; }
}
