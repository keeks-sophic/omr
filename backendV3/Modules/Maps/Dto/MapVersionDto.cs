namespace BackendV3.Modules.Maps.Dto;

public sealed class MapVersionDto
{
    public Guid MapVersionId { get; set; }
    public Guid MapId { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public string? ChangeSummary { get; set; }
    public Guid? DerivedFromMapVersionId { get; set; }
    public string? Label { get; set; }
}
