using Robot.Domain.Identity;
using Robot.Options;

namespace Robot.Services;

public class IdentityService
{
    public RobotIdentity Resolve(RobotOptions robotOptions, string? idOverride, string? nameOverride, string? ipOverride)
    {
        var id = idOverride ?? robotOptions.Id ?? "unknown";
        var name = nameOverride ?? robotOptions.Name;
        var ip = ipOverride ?? robotOptions.Ip;
        return new RobotIdentity(id, name, ip);
    }
}
