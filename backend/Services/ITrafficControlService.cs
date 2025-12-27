using backend.DTOs;

namespace backend.Services;

public interface ITrafficControlService
{
    void AssignRoute(global::backend.DTOs.RouteAssignDto route);
    global::backend.DTOs.GuidanceResponseDto Guide(global::backend.DTOs.GuidanceRequestDto req);
    void Release(string robotIp, int? pathId = null, int? nodeId = null);
}
