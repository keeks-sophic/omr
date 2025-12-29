using System.Collections.Generic;

namespace Backend.Dto;

public class MapDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<NodeDto>? Nodes { get; set; }
    public List<PathDto>? Paths { get; set; }
    public List<MapPointDto>? Points { get; set; }
    public List<QrDto>? Qrs { get; set; }
}
