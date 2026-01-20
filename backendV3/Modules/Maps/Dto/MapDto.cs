namespace BackendV3.Modules.Maps.Dto;

public sealed class MapDto
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ActiveMapVersionId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

