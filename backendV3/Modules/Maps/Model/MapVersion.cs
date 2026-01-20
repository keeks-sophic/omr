namespace BackendV3.Modules.Maps.Model;

public sealed class MapVersion
{
    public Guid MapVersionId { get; set; }
    public Guid MapId { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = MapVersionStatuses.Draft;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public string? ChangeSummary { get; set; }
    public Guid? DerivedFromMapVersionId { get; set; }
    public string? Label { get; set; }
}
