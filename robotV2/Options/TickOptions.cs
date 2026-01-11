namespace Robot.Options;

public class TickOptions
{
    public int HeartbeatSeconds { get; set; } = 5;
    public int SnapshotSeconds { get; set; } = 2;
    public int MotionMs { get; set; } = 50;
    public int RouteSeconds { get; set; } = 1;
    public int BatterySeconds { get; set; } = 30;
    public int HealthSeconds { get; set; } = 10;
}
