namespace BackendV3.Modules.Maps.Model;

public sealed class Map
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? ActiveMapVersionId { get; set; }
}

