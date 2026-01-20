namespace BackendV3.Modules.Maps.Dto;

public sealed class MapSnapshotDto
{
    public MapVersionDto Version { get; set; } = new();
    public NodeDto[] Nodes { get; set; } = Array.Empty<NodeDto>();
    public PathDto[] Paths { get; set; } = Array.Empty<PathDto>();
    public MapPointDto[] Points { get; set; } = Array.Empty<MapPointDto>();
    public QrDto[] Qrs { get; set; } = Array.Empty<QrDto>();
}

