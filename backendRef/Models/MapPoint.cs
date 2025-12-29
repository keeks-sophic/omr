namespace backend.Models;

public class MapPoint
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int? PathId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "poi";
    public double Offset { get; set; }
    public Map? Map { get; set; }
    public Path? Path { get; set; }
}
