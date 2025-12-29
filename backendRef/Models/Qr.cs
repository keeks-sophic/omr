using NetTopologySuite.Geometries;

namespace backend.Models;

public class Qr
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int? PathId { get; set; }
    public string Data { get; set; } = string.Empty;
    public double OffsetStart { get; set; }
    public Point? Location { get; set; }
    public Map? Map { get; set; }
    public Path? Path { get; set; }
}
