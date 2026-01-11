namespace Robot.Domain.Identity;

public class IdentityResolver
{
    public RobotIdentity Resolve(string id, string? name, string? ip) => new(id, name, ip);
}

