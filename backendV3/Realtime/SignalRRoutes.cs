namespace BackendV3.Realtime;

public static class SignalRRoutes
{
    public const string RealtimeHubPath = "/hubs/realtime";

    public static class Events
    {
        public const string MapVersionCreated = "map.version.created";
        public const string MapVersionPublished = "map.version.published";
        public const string MapEntityUpdated = "map.entity.updated";
    }
}
