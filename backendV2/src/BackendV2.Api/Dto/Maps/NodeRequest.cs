using System;

namespace BackendV2.Api.Dto.Maps;

public class NodeRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid MapVersionId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsActive { get; set; } = true;
}
