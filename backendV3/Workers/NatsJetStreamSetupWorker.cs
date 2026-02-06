using BackendV3.Infrastructure.Messaging;
using BackendV3.Messaging;
using Microsoft.Extensions.Hosting;
using NATS.Client.JetStream;

namespace BackendV3.Workers;

public sealed class NatsJetStreamSetupWorker : IHostedService
{
    private readonly NatsConnection _nats;

    public NatsJetStreamSetupWorker(NatsConnection nats)
    {
        _nats = nats;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var conn = _nats.Get();
        var jsm = conn.CreateJetStreamManagementContext();

        EnsureStream(jsm, NatsJetStreamRoutes.Streams.RobotsIn, new[]
        {
            NatsJetStreamRoutes.Subjects.IdentityAll,
            NatsJetStreamRoutes.Subjects.CapabilityAll,
            NatsJetStreamRoutes.Subjects.StatusAll,
            NatsJetStreamRoutes.Subjects.TelemetryAll,
            NatsJetStreamRoutes.Subjects.SettingsReportedAll,
            NatsJetStreamRoutes.Subjects.CommandAckAll
        });

        EnsureStream(jsm, NatsJetStreamRoutes.Streams.RobotsOut, new[]
        {
            NatsJetStreamRoutes.Subjects.SettingsDesiredAll,
            NatsJetStreamRoutes.Subjects.CommandAll
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void EnsureStream(IJetStreamManagement jsm, string name, string[] subjects)
    {
        try
        {
            jsm.GetStreamInfo(name);
        }
        catch
        {
            var sc = StreamConfiguration.Builder()
                .WithName(name)
                .WithSubjects(subjects)
                .Build();
            jsm.AddStream(sc);
        }
    }
}

