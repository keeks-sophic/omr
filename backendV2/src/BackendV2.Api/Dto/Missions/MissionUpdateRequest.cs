using System.Collections.Generic;

namespace BackendV2.Api.Dto.Missions;

public class MissionUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public List<MissionStepDto> Steps { get; set; } = new();
}
