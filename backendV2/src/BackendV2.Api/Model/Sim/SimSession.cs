using System;

namespace BackendV2.Api.Model.Sim;

public class SimSession
{
    public Guid SimSessionId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double SpeedMultiplier { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string ConfigJson { get; set; } = "{}";
}
