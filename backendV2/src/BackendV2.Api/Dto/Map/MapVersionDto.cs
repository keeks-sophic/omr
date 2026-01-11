using System;

namespace BackendV2.Api.Dto.Map;

public class MapVersionDto
{
    public Guid MapVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? ChangeSummary { get; set; }
}
