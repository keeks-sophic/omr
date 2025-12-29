namespace backend.Repositories;

public interface IRouteRepository
{
    int? GetNearestNodeId(int mapId, double x, double y);
    (int[] nodeIds, int[] pathIds, double totalLength)? ComputeRoute(int mapId, int startNodeId, int destNodeId);
}
