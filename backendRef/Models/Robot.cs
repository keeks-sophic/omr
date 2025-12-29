namespace backend.Models;

public class Robot
{
    public int Id { get; set; }
    public NetTopologySuite.Geometries.Point? Geom { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string State { get; set; } = "idle";
    public double Battery { get; set; }
    
    public bool Connected { get; set; }
    public DateTime? LastActive { get; set; }
    public int? MapId { get; set; }
}
