namespace BackendV2.Api.SignalR;

public class RealtimeAuthorizer
{
    public bool IsAllowed(string role, string robotId, string[]? allowedRobotIds)
    {
        if (allowedRobotIds is { Length: > 0 } && robotId != null)
        {
            for (var i = 0; i < allowedRobotIds.Length; i++)
            {
                if (allowedRobotIds[i] == robotId) return true;
            }
            return false;
        }
        return true;
    }
}
