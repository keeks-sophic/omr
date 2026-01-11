using System;
using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Service.Schedule;
using Microsoft.Extensions.Hosting;

namespace BackendV2.Api.Workers;

public class SchedulePublisherWorker : BackgroundService
{
    private readonly SchedulePublisherService _publisher;
    public SchedulePublisherWorker(SchedulePublisherService publisher)
    {
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _publisher.PublishSchedulesAsync();
            try
            {
                var trafficField = typeof(SchedulePublisherService).GetField("_traffic", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (trafficField?.GetValue(_publisher) is BackendV2.Api.Service.Traffic.TrafficControlService traffic)
                {
                    await traffic.EmitScheduleSummaryAsync();
                }
            }
            catch { }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
