namespace BackendV2.Api.Dto.Missions;

public class MissionStepDto
{
    public string Action { get; set; } = string.Empty;
    public object Parameters { get; set; } = new { };
}
