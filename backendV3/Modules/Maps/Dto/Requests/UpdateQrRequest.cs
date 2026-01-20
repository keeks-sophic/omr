namespace BackendV3.Modules.Maps.Dto.Requests;

public sealed class UpdateQrRequest
{
    public string PathId { get; set; } = string.Empty;
    public double DistanceAlongPath { get; set; }
    public string QrCode { get; set; } = string.Empty;
}

