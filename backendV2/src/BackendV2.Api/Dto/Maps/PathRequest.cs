using System;

namespace BackendV2.Api.Dto.Maps;

public class PathRequest
{
    public Guid MapVersionId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public bool TwoWay { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public double? SpeedLimit { get; set; }
    public bool IsRestPath { get; set; }
    public int? RestCapacity { get; set; }
}
