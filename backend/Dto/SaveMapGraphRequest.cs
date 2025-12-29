using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend.Dto;

public class SaveMapGraphRequest
{
    public MapDto Map { get; set; } = new MapDto();
    [JsonPropertyName("maps")]
    public MapDto? Maps { get; set; }
    public List<NodeDto> Nodes { get; set; } = new List<NodeDto>();
    public List<PathDto> Paths { get; set; } = new List<PathDto>();
    public List<MapPointDto> Points { get; set; } = new List<MapPointDto>();
    public List<QrDto> Qrs { get; set; } = new List<QrDto>();
}
