using backend.Models;

namespace backend.Services;

public interface IRobotService
{
    IEnumerable<Robot> GetAll();
    Robot? GetByIp(string ip);
    Robot UpdateTelemetry(string ip, bool update,string? name, double? x, double? y, string? state, double? battery, int? mapId = null);
    void MarkDisconnected(string? ip);
    void UnassignFromMap(string ip);
}
