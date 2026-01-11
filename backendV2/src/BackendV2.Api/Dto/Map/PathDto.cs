using System;
using System.Collections.Generic;
using BackendV2.Api.Dto.Routes;

namespace BackendV2.Api.Dto.Map;

public class PathDto
{
    public Guid PathId { get; set; }
    public Guid MapVersionId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public bool TwoWay { get; set; }
    public bool IsActive { get; set; }
    public bool IsMaintenance { get; set; }
    public double? SpeedLimit { get; set; }
    public bool IsRestPath { get; set; }
    public int? RestCapacity { get; set; }
    public object? RestDwellPolicy { get; set; }
    public List<PointDto> Points { get; set; } = new List<PointDto>();
}
