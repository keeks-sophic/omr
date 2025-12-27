using System.Collections.Generic;

namespace backend.Models;

public class Map
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Node> Nodes { get; set; } = new List<Node>();
    public ICollection<Path> Paths { get; set; } = new List<Path>();
    public ICollection<MapPoint> MapPoints { get; set; } = new List<MapPoint>();
    public ICollection<Qr> Qrs { get; set; } = new List<Qr>();
}
