namespace BackendV3.Modules.Maps.Dto.Requests;

public sealed class CreatePathRequest
{
    public string? PathId { get; set; }
    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public string Direction { get; set; } = "TWO_WAY";
    public double? SpeedLimit { get; set; }
}

