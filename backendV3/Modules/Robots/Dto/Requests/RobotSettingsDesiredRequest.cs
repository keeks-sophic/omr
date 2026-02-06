using System.Text.Json;

namespace BackendV3.Modules.Robots.Dto.Requests;

public sealed class RobotSettingsDesiredRequest
{
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
}

