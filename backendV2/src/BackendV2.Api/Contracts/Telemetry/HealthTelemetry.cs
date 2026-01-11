namespace BackendV2.Api.Contracts.Telemetry;

public class HealthTelemetry
{
    public double? Temperature { get; set; }
    public string[]? MotorFaults { get; set; }
    public string? LastError { get; set; }
}
