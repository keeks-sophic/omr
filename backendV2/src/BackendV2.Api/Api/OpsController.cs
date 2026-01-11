using System.Threading.Tasks;
using BackendV2.Api.Service.Ops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendV2.Api.Api;

[ApiController]
[Route("api/v1/ops")]
public class OpsController : ControllerBase
{
    [Authorize]
    [HttpGet("health")]
    public async Task<IActionResult> Health([FromServices] OpsService ops)
    {
        var h = await ops.GetHealthAsync();
        return Ok(h);
    }

    [Authorize]
    [HttpGet("jetstream")]
    public async Task<IActionResult> JetStream([FromServices] OpsService ops)
    {
        var js = await ops.GetJetStreamAsync();
        return Ok(js);
    }

    [Authorize]
    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts([FromServices] OpsService ops)
    {
        var alerts = await ops.GetAlertsAsync();
        return Ok(alerts);
    }

    [Authorize]
    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromServices] OpsService ops)
    {
        var events = await ops.GetAuditAsync();
        return Ok(events);
    }
}
