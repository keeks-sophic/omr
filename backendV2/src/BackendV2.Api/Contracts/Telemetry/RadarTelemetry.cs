namespace BackendV2.Api.Contracts.Telemetry;

public class RadarTelemetry
{
    public bool ObstacleDetected { get; set; }
    public double? Distance { get; set; }
}
