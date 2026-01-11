using System;

namespace BackendV2.Api.Model.Traffic;

public class TrafficHold
{
    public Guid HoldId { get; set; }
    public Guid MapVersionId { get; set; }
    public Guid? NodeId { get; set; }
    public Guid? PathId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
