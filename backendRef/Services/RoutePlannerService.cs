using backend.DTOs;
using backend.Repositories;

namespace backend.Services;

public class RoutePlannerService : IRoutePlannerService
{
    private readonly IMapRepository _maps;
    private readonly IRobotService _robots;
    private readonly IRouteRepository _routesRepo;

    public RoutePlannerService(IMapRepository maps, IRobotService robots, IRouteRepository routesRepo)
    {
        _maps = maps;
        _robots = robots;
        _routesRepo = routesRepo;
    }

    public RoutePlanDto? Plan(RouteRequestDto req)
    {
        var map = _maps.FindByIdWithGraph(req.MapId);
        if (map is null) return null;
        var robot = string.IsNullOrWhiteSpace(req.RobotIp) ? null : _robots.GetByIp(req.RobotIp);

        double sx, sy;
        if (robot?.Geom is not null)
        {
            sx = robot.X;
            sy = robot.Y;
        }
        else if (req.StartX.HasValue && req.StartY.HasValue)
        {
            sx = req.StartX.Value;
            sy = req.StartY.Value;
        }
        else
        {
            return null;
        }

        double dx = sx, dy = sy;
        if (req.DestX.HasValue && req.DestY.HasValue)
        {
            dx = req.DestX.Value;
            dy = req.DestY.Value;
        }

        var startNodeId = _routesRepo.GetNearestNodeId(req.MapId, sx, sy);
        if (!startNodeId.HasValue) return null;

        int destNodeId;
        if (req.DestinationNodeId.HasValue)
        {
            destNodeId = req.DestinationNodeId.Value;
        }
        else
        {
            var dn = _routesRepo.GetNearestNodeId(req.MapId, dx, dy);
            if (!dn.HasValue) return null;
            destNodeId = dn.Value;
        }

        var route = _routesRepo.ComputeRoute(req.MapId, startNodeId.Value, destNodeId);
        if (route is null) return null;
        var (nodeIds, pathIds, totalLength) = route.Value;

        return new RoutePlanDto
        {
            MapId = map.Id,
            RobotIp = req.RobotIp,
            StartNodeId = startNodeId.Value,
            DestinationNodeId = destNodeId,
            NodeIds = nodeIds,
            PathIds = pathIds,
            TotalLength = totalLength,
            Mode = req.Mode
        };
    }
}
