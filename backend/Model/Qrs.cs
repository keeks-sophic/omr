using NetTopologySuite.Geometries;

namespace Backend.Model;

public class Qrs
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int PathId { get; set; }
    public string Data { get; set; } = string.Empty;
    public Point? Location { get; set; }
    public double OffsetStart { get; set; }
}
