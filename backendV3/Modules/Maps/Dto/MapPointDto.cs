namespace BackendV3.Modules.Maps.Dto;

public sealed class MapPointDto
{
    public Guid PointId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public GeomDto Geom { get; set; } = new();
    public Guid? AttachedNodeId { get; set; }
}

