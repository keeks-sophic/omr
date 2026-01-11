using System;

namespace BackendV2.Api.Dto.Map;

public class NodeDto
{
    public Guid NodeId { get; set; }
    public Guid MapVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsActive { get; set; }
    public bool IsMaintenance { get; set; }
    public object? Metadata { get; set; }
}
