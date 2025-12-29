using NetTopologySuite.Geometries;

namespace Backend.Model;

public class Points
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int PathId { get; set; }
    public double Offset { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Point? Location { get; set; }
}
