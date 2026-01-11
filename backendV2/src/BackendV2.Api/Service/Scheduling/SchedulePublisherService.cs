using System.Threading.Tasks;

namespace BackendV2.Api.Service.Scheduling;

public class SchedulePublisherService
{
    public Task<object> HealthAsync() => Task.FromResult<object>(new { ok = true });
}
