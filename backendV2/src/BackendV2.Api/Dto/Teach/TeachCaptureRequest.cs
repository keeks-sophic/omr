namespace BackendV2.Api.Dto.Teach;

public class TeachCaptureRequest
{
    public string CorrelationId { get; set; } = string.Empty;
    public object RobotState { get; set; } = new { };
}
