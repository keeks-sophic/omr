using System;

namespace BackendV2.Api.Dto.Teach;

public class TeachSessionCreateRequest
{
    public string RobotId { get; set; } = string.Empty;
    public Guid MapVersionId { get; set; }
}
