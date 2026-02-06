using System.Text.Json;

namespace BackendV3.Modules.Robots.Dto;

public sealed class RobotIdentityDto
{
    public string RobotId { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
}

