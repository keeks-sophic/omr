namespace BackendV3.Modules.Maps.Dto;

public sealed class MapDto
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid? ActivePublishedMapVersionId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
