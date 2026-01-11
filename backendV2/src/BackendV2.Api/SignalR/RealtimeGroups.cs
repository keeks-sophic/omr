namespace BackendV2.Api.SignalR;

public static class RealtimeGroups
{
    public const string Robots = "robots";
    public static string Robot(string robotId) => $"robot:{robotId}";
}
