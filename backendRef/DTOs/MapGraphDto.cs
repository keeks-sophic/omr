namespace backend.DTOs;

public class MapGraphDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MapNodeDto> Nodes { get; set; } = new();
    public List<MapPathDto> Paths { get; set; } = new();
    public List<MapPointDto> Points { get; set; } = new();
    public List<QrDto> Qrs { get; set; } = new();
}

public class MapNodeDto
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public class MapPathDto
{
    public int Id { get; set; }
    public int StartNodeId { get; set; }
    public int EndNodeId { get; set; }
    public bool TwoWay { get; set; }
}

public class MapPointDto
{
    public int Id { get; set; }
    public int? PathId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "poi";
    public double Offset { get; set; }
}

public class QrDto
{
    public int Id { get; set; }
    public int? PathId { get; set; }
    public string Data { get; set; } = string.Empty;
    public double OffsetStart { get; set; }
}
