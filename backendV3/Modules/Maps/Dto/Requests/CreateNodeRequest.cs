namespace BackendV3.Modules.Maps.Dto.Requests;

public sealed class CreateNodeRequest
{
    public string? NodeId { get; set; }
    public GeomDto Geom { get; set; } = new();
    public string Label { get; set; } = string.Empty;
    public double? JunctionSpeedLimit { get; set; }
}

