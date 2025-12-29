namespace backend.DTOs;

public class GuidanceResponseDto
{
    public bool Allow { get; set; }
    public string? Reason { get; set; }
    public int HoldMs { get; set; }
    public double AheadDistance { get; set; }
}
