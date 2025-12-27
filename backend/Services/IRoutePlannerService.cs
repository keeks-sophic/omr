using backend.DTOs;

namespace backend.Services;

public interface IRoutePlannerService
{
    RoutePlanDto? Plan(RouteRequestDto req);
}
