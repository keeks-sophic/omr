using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Messaging;
using BackendV2.Api.Service.Sim;
using BackendV2.Api.Topics;
using Microsoft.Extensions.Hosting;
using NATS.Client;

namespace BackendV2.Api.Workers;

public class BackendSimControlWorker : BackgroundService
{
    private readonly NatsConnection _nats;
    private readonly SimulationService _sim;
    private IConnection? _conn;
    private IAsyncSubscription? _start;
    private IAsyncSubscription? _stop;
    public BackendSimControlWorker(NatsConnection nats, SimulationService sim)
    {
        _nats = nats;
        _sim = sim;
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _conn = _nats.Get();
        _start = _conn.SubscribeAsync(NatsTopics.BackendSimStart(), async (s, a) =>
        {
            try
            {
                var payload = JsonSerializer.Deserialize<object>(a.Message.Data);
                var id = ExtractGuid(payload, "simSessionId");
                if (id != Guid.Empty) await _sim.StartAsync(id);
            }
            catch { }
        });
        _stop = _conn.SubscribeAsync(NatsTopics.BackendSimStop(), async (s, a) =>
        {
            try
            {
                var payload = JsonSerializer.Deserialize<object>(a.Message.Data);
                var id = ExtractGuid(payload, "simSessionId");
                if (id != Guid.Empty) await _sim.StopAsync(id);
            }
            catch { }
        });
        return Task.CompletedTask;
    }
    private static Guid ExtractGuid(object? parameters, string property)
    {
        try
        {
            var json = JsonSerializer.Serialize(parameters);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var g))
                return g;
        }
        catch { }
        return Guid.Empty;
    }
}
