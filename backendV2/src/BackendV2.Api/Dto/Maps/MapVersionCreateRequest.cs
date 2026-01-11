namespace BackendV2.Api.Dto.Maps;

public class MapVersionCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
}
