namespace BackendV3.Modules.Maps.Dto;

public sealed class QrDto
{
    public Guid QrId { get; set; }
    public Guid MapVersionId { get; set; }
    public Guid PathId { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public double DistanceAlongPath { get; set; }
}

