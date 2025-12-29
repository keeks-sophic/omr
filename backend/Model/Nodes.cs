using NetTopologySuite.Geometries;

namespace Backend.Model;

public class Nodes
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public Point? Location { get; set; }
    public string Status { get; set; } = string.Empty;
}
