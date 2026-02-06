using BackendV3.Modules.Robots.Data;
using BackendV3.Modules.Robots.Dto;
using BackendV3.Modules.Robots.Mapping;

namespace BackendV3.Modules.Robots.Service;

public sealed class RobotRegistryService
{
    private readonly RobotRepository _robots;
    private readonly RobotIdentityRepository _identity;
    private readonly RobotCapabilityRepository _capability;
    private readonly RobotSettingsRepository _settings;

    public RobotRegistryService(
        RobotRepository robots,
        RobotIdentityRepository identity,
        RobotCapabilityRepository capability,
        RobotSettingsRepository settings)
    {
        _robots = robots;
        _identity = identity;
        _capability = capability;
        _settings = settings;
    }

    public Task<List<Model.Robot>> ListAsync(CancellationToken ct = default) => _robots.ListAsync(ct);

    public Task<Model.Robot?> GetAsync(string robotId, CancellationToken ct = default) => _robots.GetAsync(robotId, ct);

    public async Task<RobotDto?> GetDtoAsync(string robotId, CancellationToken ct = default)
    {
        var robot = await _robots.GetAsync(robotId, ct);
        if (robot == null) return null;
        var identity = await _identity.GetLatestAsync(robotId, ct);
        var capability = await _capability.GetLatestAsync(robotId, ct);
        var reported = await _settings.GetLatestReportedAsync(robotId, ct);
        return RobotMapper.ToDto(robot, identity, capability, reported);
    }

    public async Task<List<RobotDto>> ListDtosAsync(CancellationToken ct = default)
    {
        var robots = await _robots.ListAsync(ct);
        var dtos = new List<RobotDto>(robots.Count);
        foreach (var robot in robots)
        {
            var identity = await _identity.GetLatestAsync(robot.RobotId, ct);
            var capability = await _capability.GetLatestAsync(robot.RobotId, ct);
            var reported = await _settings.GetLatestReportedAsync(robot.RobotId, ct);
            dtos.Add(RobotMapper.ToDto(robot, identity, capability, reported));
        }
        return dtos;
    }

    public Task EnsureExistsAsync(string robotId, CancellationToken ct = default) => _robots.EnsureExistsAsync(robotId, ct);

    public Task UpdateMetadataAsync(string robotId, string? displayName, bool? isEnabled, string? tagsJson, string? notes, CancellationToken ct = default) =>
        _robots.UpdateMetadataAsync(robotId, displayName, isEnabled, tagsJson, notes, ct);
}

