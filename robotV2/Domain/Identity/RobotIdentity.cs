namespace Robot.Domain.Identity;

public class RobotIdentity
{
    public string Id { get; }
    public string? Name { get; }
    public string? Ip { get; }

    public RobotIdentity(string id, string? name, string? ip)
    {
        Id = id;
        Name = name;
        Ip = ip;
    }
}

