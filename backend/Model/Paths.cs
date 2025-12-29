using NetTopologySuite.Geometries;

namespace Backend.Model;

public class Paths
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int StartNodeId { get; set; }
    public int EndNodeId { get; set; }
    public LineString? Location { get; set; }
    public bool TwoWay { get; set; }
    public double Length { get; set; }
    public string Status { get; set; } = string.Empty;
}
