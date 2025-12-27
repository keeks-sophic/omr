using NetTopologySuite.Geometries;

namespace backend.Models;

public class Path
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int StartNodeId { get; set; }
    public int EndNodeId { get; set; }
    public bool TwoWay { get; set; }
    public string Status { get; set; } = "open";
    public double Length { get; set; }
    public LineString? Location { get; set; }
    public Map? Map { get; set; }
    public Node? StartNode { get; set; }
    public Node? EndNode { get; set; }
}
