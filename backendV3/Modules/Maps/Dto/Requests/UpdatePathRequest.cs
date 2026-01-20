namespace BackendV3.Modules.Maps.Dto.Requests;

public sealed class UpdatePathRequest
{
    public string Direction { get; set; } = "TWO_WAY";
    public double? SpeedLimit { get; set; }
}

