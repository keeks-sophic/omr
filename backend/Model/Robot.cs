using System;
using NetTopologySuite.Geometries;

namespace Backend.Model;

public class Robot
{
    public int Id { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public Point? Location { get; set; }
    public string State { get; set; } = string.Empty;
    public double Battery { get; set; }
    public bool Connected { get; set; }
    public DateTime LastActive { get; set; }
    public int MapId { get; set; }
}
