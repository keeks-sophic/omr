namespace Robot.Domain.State;

public class HealthState
{
    public double? BatteryPct { get; set; }
    public double? BatteryVoltage { get; set; }
    public double? Temperature { get; set; }
    public string[]? MotorFaults { get; set; }
    public string? LastError { get; set; }
}

