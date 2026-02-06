using System.Text.Json;

namespace BackendV3.Modules.Robots.Dto.Requests;

public sealed class RobotUpdateRequest
{
    public string? DisplayName { get; set; }
    public bool? IsEnabled { get; set; }
    public JsonDocument? Tags { get; set; }
    public string? Notes { get; set; }
}

