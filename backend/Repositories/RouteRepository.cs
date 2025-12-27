using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class RouteRepository : IRouteRepository
{
    private readonly ApplicationDbContext _db;
    public RouteRepository(ApplicationDbContext db) { _db = db; }

    public int? GetNearestNodeId(int mapId, double x, double y)
    {
        var nodes = _db.Nodes.AsNoTracking().Where(n => n.MapId == mapId).ToArray();
        if (nodes.Length == 0) return null;
        var best = nodes.Select(n => (n, d: Math.Sqrt(Math.Pow(n.X - x, 2) + Math.Pow(n.Y - y, 2)))).OrderBy(t => t.d).First();
        return best.n.Id;
    }

    public (int[] nodeIds, int[] pathIds, double totalLength)? ComputeRoute(int mapId, int startNodeId, int destNodeId)
    {
        var nodes = _db.Nodes.AsNoTracking().Where(n => n.MapId == mapId).ToDictionary(n => n.Id);
        var paths = _db.Paths.AsNoTracking().Where(p => p.MapId == mapId).ToArray();
        var adj = BuildAdjacency(paths);
        var nodePath = Dijkstra(startNodeId, destNodeId, adj);
        if (nodePath is null) return null;
        var pathIds = ToPathIds(paths, nodePath);
        var length = ComputeLength(paths, nodePath);
        return (nodePath, pathIds, length);
    }

    private static Dictionary<int, List<(int to, double w)>> BuildAdjacency(Path[] paths)
    {
        var adj = new Dictionary<int, List<(int to, double w)>>();
        void Add(int a, int b, double w)
        {
            if (!adj.TryGetValue(a, out var list))
            {
                list = new List<(int to, double w)>();
                adj[a] = list;
            }
            list.Add((b, w));
        }
        foreach (var p in paths)
        {
            var w = p.Location != null ? Math.Max(0.0001, p.Location.Length) : Math.Max(0.0001, p.Length);
            Add(p.StartNodeId, p.EndNodeId, w);
            if (p.TwoWay) Add(p.EndNodeId, p.StartNodeId, w);
        }
        return adj;
    }

    private static int[]? Dijkstra(int start, int dest, Dictionary<int, List<(int to, double w)>> adj)
    {
        var dist = new Dictionary<int, double>();
        var prev = new Dictionary<int, int>();
        var pq = new PriorityQueue<int, double>();
        dist[start] = 0;
        pq.Enqueue(start, 0);
        var visited = new HashSet<int>();
        while (pq.Count > 0)
        {
            pq.TryDequeue(out var u, out var _);
            if (!visited.Add(u)) continue;
            if (u == dest) break;
            if (!adj.TryGetValue(u, out var edges)) continue;
            foreach (var (v, w) in edges)
            {
                var nd = dist[u] + w;
                if (!dist.TryGetValue(v, out var dv) || nd < dv)
                {
                    dist[v] = nd;
                    prev[v] = u;
                    pq.Enqueue(v, nd);
                }
            }
        }
        if (!prev.ContainsKey(dest) && start != dest) return null;
        var path = new List<int>();
        var cur = dest;
        path.Add(cur);
        while (cur != start)
        {
            if (!prev.TryGetValue(cur, out var p)) break;
            cur = p;
            path.Add(cur);
        }
        path.Reverse();
        return path.ToArray();
    }

    private static int[] ToPathIds(Path[] paths, int[] nodeIds)
    {
        var res = new List<int>();
        for (var i = 0; i + 1 < nodeIds.Length; i++)
        {
            var a = nodeIds[i];
            var b = nodeIds[i + 1];
            var p = paths.FirstOrDefault(x => x.StartNodeId == a && x.EndNodeId == b)
                ?? paths.FirstOrDefault(x => x.TwoWay && x.StartNodeId == b && x.EndNodeId == a);
            if (p is not null) res.Add(p.Id);
        }
        return res.ToArray();
    }

    private static double ComputeLength(Path[] paths, int[] nodeIds)
    {
        var sum = 0.0;
        for (var i = 0; i + 1 < nodeIds.Length; i++)
        {
            var a = nodeIds[i];
            var b = nodeIds[i + 1];
            var p = paths.FirstOrDefault(x => x.StartNodeId == a && x.EndNodeId == b)
                ?? paths.FirstOrDefault(x => x.TwoWay && x.StartNodeId == b && x.EndNodeId == a);
            if (p is not null)
            {
                var segLen = p.Location != null ? p.Location.Length : p.Length;
                sum += segLen;
            }
        }
        return sum;
    }
}
