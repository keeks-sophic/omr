using NetTopologySuite.Geometries;

namespace Backend.Model;

public class Destinations
{
    public int Id { get; set; }
    public int RobotId { get; set; }
    public int MapId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public Point? Location { get; set; }
}

