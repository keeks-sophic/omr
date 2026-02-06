using BackendV3.Modules.Robots.Data;
using BackendV3.Modules.Robots.Model;

namespace BackendV3.Modules.Robots.Service;

public sealed class RobotCapabilityService
{
    private readonly RobotCapabilityRepository _capability;

    public RobotCapabilityService(RobotCapabilityRepository capability)
    {
        _capability = capability;
    }

    public Task<RobotCapabilitySnapshot?> GetLatestAsync(string robotId, CancellationToken ct = default) =>
        _capability.GetLatestAsync(robotId, ct);
}

