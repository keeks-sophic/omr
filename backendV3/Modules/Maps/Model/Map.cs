namespace BackendV3.Modules.Maps.Model;

public sealed class Map
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid? ActivePublishedMapVersionId { get; set; }
}
