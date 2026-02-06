using System.Text.Json;

namespace BackendV3.Modules.Robots.Dto.Requests;

public sealed class RobotCommandRequest
{
    public string CommandType { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
}

