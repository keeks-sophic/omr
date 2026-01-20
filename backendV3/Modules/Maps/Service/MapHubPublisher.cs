using BackendV3.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace BackendV3.Modules.Maps.Service;

public sealed class MapHubPublisher
{
    private readonly IHubContext<RealtimeHub> _hub;

    public MapHubPublisher(IHubContext<RealtimeHub> hub)
    {
        _hub = hub;
    }

    public Task MapVersionCreatedAsync(Guid mapId, Guid mapVersionId, CancellationToken ct = default)
    {
        return _hub.Clients.All.SendAsync(
            SignalRRoutes.Events.MapVersionCreated,
            new { mapId = mapId.ToString(), mapVersionId = mapVersionId.ToString() },
            ct);
    }

    public Task MapVersionPublishedAsync(Guid mapId, Guid mapVersionId, CancellationToken ct = default)
    {
        return _hub.Clients.All.SendAsync(
            SignalRRoutes.Events.MapVersionPublished,
            new { mapId = mapId.ToString(), mapVersionId = mapVersionId.ToString() },
            ct);
    }

    public Task MapEntityUpdatedAsync(Guid mapId, Guid mapVersionId, string entityType, Guid id, CancellationToken ct = default)
    {
        return _hub.Clients.All.SendAsync(
            SignalRRoutes.Events.MapEntityUpdated,
            new { mapId = mapId.ToString(), mapVersionId = mapVersionId.ToString(), entityType, id = id.ToString() },
            ct);
    }
}
