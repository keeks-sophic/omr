using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using backend.Repositories;

namespace backend.Services;

public class TrafficControlService : ITrafficControlService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _lock = new();
    private readonly Dictionary<string, (int pathId, double offset, int dir, int mapId)> _robotSeg = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, (int? dirLock, HashSet<string> holders)> _pathLocks = new();
    private readonly Dictionary<int, string> _nodeLocks = new(); // nodeId -> robotIp
    private readonly Dictionary<string, global::backend.DTOs.RouteAssignDto> _routes = new(StringComparer.OrdinalIgnoreCase);

    public TrafficControlService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void AssignRoute(global::backend.DTOs.RouteAssignDto route)
    {
        lock (_lock)
        {
            _routes[route.RobotIp] = route;
        }
    }

    public global::backend.DTOs.GuidanceResponseDto Guide(global::backend.DTOs.GuidanceRequestDto req)
    {
        lock (_lock)
        {
            _robotSeg[req.RobotIp] = (req.PathId, req.Offset, req.Direction, req.MapId);

            // Collision check: same path, same direction, robot ahead within 0.2m
            var ahead = _robotSeg.Where(kv => kv.Value.pathId == req.PathId && kv.Value.dir == req.Direction && !string.Equals(kv.Key, req.RobotIp, StringComparison.OrdinalIgnoreCase))
                                 .Select(kv => kv.Value.offset - req.Offset)
                                 .Where(d => d >= 0)
                                 .DefaultIfEmpty(double.MaxValue)
                                 .Min();
            if (ahead <= 0.2)
            {
                return new global::backend.DTOs.GuidanceResponseDto { Allow = false, Reason = "robot_ahead", HoldMs = 200, AheadDistance = ahead };
            }

            // Simple path lock without DB
            if (!_pathLocks.TryGetValue(req.PathId, out var lockInfo))
            {
                lockInfo = (null, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                _pathLocks[req.PathId] = lockInfo;
            }
            var dirLock = lockInfo.dirLock;
            var holders = lockInfo.holders;
            if (dirLock.HasValue && dirLock.Value != req.Direction && holders.Count > 0)
            {
                return new global::backend.DTOs.GuidanceResponseDto { Allow = false, Reason = "opposite_direction_on_path", HoldMs = 300 };
            }
            lockInfo.dirLock = req.Direction;
            holders.Add(req.RobotIp);
            _pathLocks[req.PathId] = lockInfo;

            // Node lock: ensure next node free before entering
            var nextNodeId = req.NextNodeId;
            if (_nodeLocks.TryGetValue(nextNodeId, out var holder) && !string.Equals(holder, req.RobotIp, StringComparison.OrdinalIgnoreCase))
            {
                return new GuidanceResponseDto { Allow = false, Reason = "node_occupied", HoldMs = 300 };
            }
            // Reserve node for caller approaching
            _nodeLocks[nextNodeId] = req.RobotIp;

            return new global::backend.DTOs.GuidanceResponseDto { Allow = true, HoldMs = 0, AheadDistance = double.IsInfinity(ahead) ? -1 : ahead };
        }
    }

    public void Release(string robotIp, int? pathId = null, int? nodeId = null)
    {
        lock (_lock)
        {
            if (pathId.HasValue && _pathLocks.TryGetValue(pathId.Value, out var lockInfo))
            {
                lockInfo.holders.Remove(robotIp);
                if (lockInfo.holders.Count == 0)
                {
                    lockInfo.dirLock = null;
                }
                _pathLocks[pathId.Value] = lockInfo;
            }
            if (nodeId.HasValue && _nodeLocks.TryGetValue(nodeId.Value, out var holder) && string.Equals(holder, robotIp, StringComparison.OrdinalIgnoreCase))
            {
                _nodeLocks.Remove(nodeId.Value);
            }
        }
    }
}
