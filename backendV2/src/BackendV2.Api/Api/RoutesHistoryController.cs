using System;
using System.Linq;
using System.Threading.Tasks;
using BackendV2.Api.Service.Timescale;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/routes/{routeId}/progress")]
public class RoutesHistoryController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Get(string routeId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int? limit, [FromServices] TimescaleQueryService ts)
    {
        var list = await ts.GetRouteProgressAsync(routeId, from, to, limit ?? 500);
        return Ok(list.Select(x => new { timestamp = x.Ts, routeId = x.RouteId, robotId = x.RobotId, segmentIndex = x.SegmentIndex, distanceAlong = x.DistanceAlong, eta = x.Eta }));
    }
}
