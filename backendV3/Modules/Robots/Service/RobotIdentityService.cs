using BackendV3.Modules.Robots.Data;
using BackendV3.Modules.Robots.Model;

namespace BackendV3.Modules.Robots.Service;

public sealed class RobotIdentityService
{
    private readonly RobotIdentityRepository _identity;

    public RobotIdentityService(RobotIdentityRepository identity)
    {
        _identity = identity;
    }

    public Task<RobotIdentitySnapshot?> GetLatestAsync(string robotId, CancellationToken ct = default) =>
        _identity.GetLatestAsync(robotId, ct);
}

