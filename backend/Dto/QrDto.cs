namespace Backend.Dto;

public class QrDto
{
    public int Id { get; set; }
    public int MapId { get; set; }
    public int PathId { get; set; }
    public string Data { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double OffsetStart { get; set; }
}
