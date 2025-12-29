using backend.Models;
using backend.Repositories;
using NetTopologySuite.Geometries;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class RobotService : IRobotService
{
    private readonly IRobotRepository _repo;
    private readonly ILogger<RobotService> _logger;
    private static readonly ConcurrentDictionary<string, Robot> _cache = new(StringComparer.OrdinalIgnoreCase);

    public RobotService(IRobotRepository repo, ILogger<RobotService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public IEnumerable<Robot> GetAll() => _repo.GetAllNoTracking().ToArray();

    public Robot? GetByIp(string ip) =>_repo.FindByIp(ip);

    // public Robot? GetCacheIp(string ip) =>_cache.GetValueOrDefault(ip);


    public Robot UpdateTelemetry(string ip, bool update,string? name, double? x, double? y, string? state, double? battery, int? mapId = null)
    {
        var cached = _cache.GetOrAdd(ip, _ => new Robot { Ip = ip });
        var prevState = cached.State;
        ApplyTelemetry(cached, name, x, y, state, battery, mapId);
        _logger.LogInformation("cached state = {State}", cached.State);
        _logger.LogInformation("new state = {State}", state);
        _logger.LogInformation("update = {Update}", cached.State!=prevState);

        if(update || cached.State!=prevState)
        {
          Robot? r = _repo.FindByIp(ip);
          _logger.LogInformation("DB lookup by IP {Ip}: Found={Found} Name={Name} Id={Id}", ip, r is not null, r?.Name, r?.Id ?? 0);
          if(r is null)
          {
            _repo.Add(cached);
          }
          else
          {
            ApplyTelemetry(r, name, x, y, state, battery, mapId);
            _repo.Update(r);
          }
          _repo.Save();
        }

        return cached;
    }

    private static void ApplyTelemetry(Robot target, string? name, double? x, double? y, string? state, double? battery, int? mapId)
    {
        if (!string.IsNullOrWhiteSpace(name)) target.Name = name!;
        if (mapId.HasValue) target.MapId = mapId.Value;
        var hasMap = target.MapId.HasValue;
        if (hasMap && x.HasValue) target.X = x.Value;
        if (hasMap && y.HasValue) target.Y = y.Value;
        if (hasMap && x.HasValue && y.HasValue) target.Geom = new Point(target.X, target.Y) { SRID = 0 };
        if (!string.IsNullOrWhiteSpace(state)) target.State = state!;
        if (battery.HasValue) target.Battery = battery.Value;
        target.Connected = true;
        target.LastActive = DateTime.UtcNow;
    }

    public void MarkDisconnected(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return;
        Robot? r = null;
        if (!string.IsNullOrWhiteSpace(ip))
        {
            r = _repo.FindByIp(ip);
        }
       
        if (r is null) return;
     
        if (_cache.TryGetValue(ip!, out var cached))
        {
            r.X = cached.X;
            r.Y = cached.Y;
            r.State = cached.State;
            r.Battery = cached.Battery;
            r.Geom = cached.Geom;
            r.LastActive = cached.LastActive ?? DateTime.UtcNow;
        }
        r.Connected = false;
        r.LastActive = DateTime.UtcNow;
        _repo.Update(r);
        _repo.Save();
        _cache.TryRemove(ip!, out _);
    }

    public void UnassignFromMap(string ip)
    {
        var r = _repo.FindByIp(ip);
        if (r is null) return;
        r.MapId = null;
        r.Geom = null;
        r.X = 0;
        r.Y = 0;
        _repo.Update(r);
        _repo.Save();
        if (_cache.TryGetValue(ip, out var cached))
        {
            cached.MapId = null;
            cached.Geom = null;
            cached.X = 0;
            cached.Y = 0;
        }
    }
}
