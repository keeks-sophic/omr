using System;

namespace BackendV2.Api.Model.Task;

public class Mission
{
    public Guid MissionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? MetadataJson { get; set; }
    public string StepsJson { get; set; } = "[]";
}
