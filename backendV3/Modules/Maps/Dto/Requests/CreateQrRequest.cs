namespace BackendV3.Modules.Maps.Dto.Requests;

public sealed class CreateQrRequest
{
    public string? QrId { get; set; }
    public string PathId { get; set; } = string.Empty;
    public double DistanceAlongPath { get; set; }
    public string QrCode { get; set; } = string.Empty;
}

