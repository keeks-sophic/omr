namespace BackendV3.Modules.Maps.Dto.Requests;

public sealed class UpdatePointRequest
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public GeomDto Geom { get; set; } = new();
    public string? AttachedNodeId { get; set; }
}

