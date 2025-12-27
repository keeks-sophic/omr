using NetTopologySuite.Geometries;

namespace backend.Models;

public class Node
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public double X { get; set; }
    public double Y { get; set; }
    public Point? Location { get; set; }
    public Map? Map { get; set; }
}
