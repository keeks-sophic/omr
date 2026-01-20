namespace BackendV3.Modules.Maps.Dto;

public sealed class NodeDto
{
    public Guid NodeId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public GeomDto Geom { get; set; } = new();
    public bool IsMaintenance { get; set; }
    public double? JunctionSpeedLimit { get; set; }
}

