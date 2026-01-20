namespace BackendV3.Modules.Maps.Dto;

public sealed class PathDto
{
    public Guid PathId { get; set; }
    public Guid MapVersionId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string Direction { get; set; } = "TWO_WAY";
    public double? SpeedLimit { get; set; }
    public bool IsMaintenance { get; set; }
    public bool IsRestPath { get; set; }
    public int? RestCapacity { get; set; }
    public string? RestDwellPolicy { get; set; }
    public GeomDto[] Points { get; set; } = Array.Empty<GeomDto>();
}

