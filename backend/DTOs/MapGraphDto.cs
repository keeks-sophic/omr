namespace backend.DTOs;

public class MapGraphDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MapNodeDto> Nodes { get; set; } = new();
    public List<MapPathDto> Paths { get; set; } = new();
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
