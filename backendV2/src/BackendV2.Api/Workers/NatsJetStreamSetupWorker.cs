using System.Threading;
using System.Threading.Tasks;
using BackendV2.Api.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using NATS.Client;
using NATS.Client.JetStream;

namespace BackendV2.Api.Workers;

public class NatsJetStreamSetupWorker : IHostedService
{
    private readonly NatsConnection _nats;
    public NatsJetStreamSetupWorker(NatsConnection nats) { _nats = nats; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var conn = _nats.Get();
        var jsm = conn.CreateJetStreamManagementContext();
        TryAddStream(jsm, "ROBOT_CMDS", new[] {
            "robot.*.cmd.*",
            "robot.*.task.assign",
            "robot.*.task.control",
            "robot.*.route.assign",
            "robot.*.route.update",
            "robot.*.cfg.*"
        });
        TryAddStream(jsm, "ROBOT_STATE", new[] {
            "robot.*.state.*",
            "robot.*.task.event",
            "robot.*.route.progress",
            "robot.*.log.event"
        });
        TryAddStream(jsm, "ROBOT_TELEMETRY", new[] {
            "robot.*.telemetry.*"
        });
        TryAddDroppableLatestWinsStream(jsm, "ROBOT_SCHEDULE", new[] {
            "robot.*.traffic.schedule"
        });
        TryAddStream(jsm, "BACKEND_DLQ", new[] {
            "backend.deadletter"
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void TryAddStream(IJetStreamManagement jsm, string name, string[] subjects)
    {
        try
        {
            jsm.GetStreamInfo(name);
        }
        catch
        {
            var sc = StreamConfiguration.Builder().WithName(name).WithSubjects(subjects).Build();
            jsm.AddStream(sc);
        }
    }

    private static void TryAddDroppableLatestWinsStream(IJetStreamManagement jsm, string name, string[] subjects)
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
                .WithMaxMsgsPerSubject(1)
                .WithDiscardPolicy(DiscardPolicy.Old)
                .Build();
            jsm.AddStream(sc);
        }
    }
}
