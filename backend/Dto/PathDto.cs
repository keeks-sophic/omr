using System.Collections.Generic;

namespace Backend.Dto;

public class PathDto
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int StartNodeId { get; set; }
    public int EndNodeId { get; set; }
    public bool TwoWay { get; set; }
    public double Length { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Rest { get; set; }
    public List<PathPointDto>? Points { get; set; }
}

public class PathPointDto
{
    public double X { get; set; }
    public double Y { get; set; }
}
