using System;

namespace BackendV2.Api.Dto.Sim;

public class SimSessionCreateRequest
{
    public Guid MapVersionId { get; set; }
    public int Robots { get; set; } = 1;
    public double SpeedMultiplier { get; set; } = 1.0;
}
