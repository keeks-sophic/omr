using System;

namespace BackendV2.Api.Dto.Map;

public class MapPointDto
{
    public Guid PointId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public Guid? AttachedNodeId { get; set; }
}
