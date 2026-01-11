using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Threading;
using BackendV2.Api.Contracts;
using BackendV2.Api.Contracts.Routes;
using BackendV2.Api.Contracts.Tasks;
using BackendV2.Api.Contracts.Traffic;
using BackendV2.Api.Infrastructure.Messaging;
using BackendV2.Api.Topics;
using NATS.Client;
using BackendV2.Api.Contracts.Commands;

namespace BackendV2.Api.Service.Tasks;

public class NatsPublisherStub
{
    private readonly NatsConnection _nats;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<BackendV2.Api.Hub.RealtimeHub>? _hub;
    public NatsPublisherStub(NatsConnection nats, Microsoft.AspNetCore.SignalR.IHubContext<BackendV2.Api.Hub.RealtimeHub>? hub = null) { _nats = nats; _hub = hub; }

    public Task PublishTaskAssignAsync(string robotId, TaskAssignment assignment)
    {
        var env = new NatsEnvelope<TaskAssignment> { RobotId = robotId, CorrelationId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow, Payload = assignment };
        return PublishJetStreamAsync(NatsTopics.RobotTaskAssign(robotId), env);
    }

    public Task PublishRouteAssignAsync(string robotId, RouteAssign routeAssign)
    {
        var env = new NatsEnvelope<RouteAssign> { RobotId = robotId, CorrelationId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow, Payload = routeAssign };
        return PublishJetStreamAsync(NatsTopics.RobotRouteAssign(robotId), env);
    }

    public Task PublishTaskControlAsync(string robotId, string taskId, string control)
    {
        var env = new NatsEnvelope<object> { RobotId = robotId, CorrelationId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow, Payload = new { taskId, control } };
        return PublishJetStreamAsync(NatsTopics.RobotTaskControl(robotId), env);
    }

    public async Task PublishTrafficScheduleAsync(string robotId, TrafficSchedule schedule)
    {
        var conn = _nats.Get();
        var env = new NatsEnvelope<TrafficSchedule> { RobotId = robotId, CorrelationId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow, Payload = schedule };
        var data = JsonSerializer.SerializeToUtf8Bytes(env);
        var js = conn.CreateJetStreamContext();
        var tries = 0;
        Exception? last = null;
        while (tries < 3)
        {
            try
            {
                js.Publish(NatsTopics.RobotTrafficSchedule(robotId), data);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                tries++;
                await Task.Delay(50);
            }
        }
        if (_hub != null && last != null)
        {
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(BackendV2.Api.Topics.SignalRTopics.OpsAlertRaised, new object[] { new { type = "publish_failure", subject = NatsTopics.RobotTrafficSchedule(robotId), reason = last.Message } }, System.Threading.CancellationToken.None);
        }
    }

    public async Task PublishCfgMotionLimitsAsync(string robotId, BackendV2.Api.Dto.Config.MotionLimitsDto limits)
    {
        var conn = _nats.Get();
        var env = new NatsEnvelope<BackendV2.Api.Dto.Config.MotionLimitsDto> { RobotId = robotId, CorrelationId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow, Payload = limits };
        var data = JsonSerializer.SerializeToUtf8Bytes(env);
        var js = conn.CreateJetStreamContext();
        await PublishWithRetryAsync(js, NatsTopics.RobotCfgMotionLimits(robotId), data);
    }
    public async Task PublishCfgRuntimeModeAsync(string robotId, string runtimeMode)
    {
        var conn = _nats.Get();
        var env = new NatsEnvelope<object> { RobotId = robotId, CorrelationId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow, Payload = new { runtimeMode } };
        var data = JsonSerializer.SerializeToUtf8Bytes(env);
        var js = conn.CreateJetStreamContext();
        await PublishWithRetryAsync(js, NatsTopics.RobotCfgRuntimeMode(robotId), data);
    }
    public async Task PublishCfgFeaturesAsync(string robotId, BackendV2.Api.Dto.Robots.RobotFeatureFlagsDto flags)
    {
        var conn = _nats.Get();
        var env = new NatsEnvelope<BackendV2.Api.Dto.Robots.RobotFeatureFlagsDto> { RobotId = robotId, CorrelationId = Guid.NewGuid().ToString("N"), Timestamp = DateTimeOffset.UtcNow, Payload = flags };
        var data = JsonSerializer.SerializeToUtf8Bytes(env);
        var js = conn.CreateJetStreamContext();
        await PublishWithRetryAsync(js, NatsTopics.RobotCfgFeatures(robotId), data);
    }

    private async Task PublishWithRetryAsync(IJetStream js, string subject, byte[] data)
    {
        var tries = 0;
        Exception? last = null;
        while (tries < 3)
        {
            try
            {
                js.Publish(subject, data);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                tries++;
                await Task.Delay(50);
            }
        }
        if (_hub != null && last != null)
        {
            await _hub.Clients.Group(BackendV2.Api.SignalR.RealtimeGroups.Robots).SendCoreAsync(BackendV2.Api.Topics.SignalRTopics.OpsAlertRaised, new object[] { new { type = "publish_failure", subject, reason = last.Message } }, System.Threading.CancellationToken.None);
        }
    }

    private Task PublishJetStreamAsync(string subject, object envelope)
    {
        var conn = _nats.Get();
        var js = conn.CreateJetStreamContext();
        var data = JsonSerializer.SerializeToUtf8Bytes(envelope);
        var ack = js.Publish(subject, data);
        return Task.CompletedTask;
    }

    public Task<string> PublishGripCommandAsync(string robotId, GripCommand cmd)
    {
        var corr = Guid.NewGuid().ToString("N");
        var env = new NatsEnvelope<GripCommand> { RobotId = robotId, CorrelationId = corr, Timestamp = DateTimeOffset.UtcNow, Payload = cmd };
        PublishJetStreamAsync(NatsTopics.RobotCmd(robotId, "grip"), env);
        return Task.FromResult(corr);
    }
    public Task PublishHoistCommandAsync(string robotId, HoistCommand cmd)
    {
        var corr = Guid.NewGuid().ToString("N");
        var env = new NatsEnvelope<HoistCommand> { RobotId = robotId, CorrelationId = corr, Timestamp = DateTimeOffset.UtcNow, Payload = cmd };
        PublishJetStreamAsync(NatsTopics.RobotCmd(robotId, "hoist"), env);
        return Task.FromResult(corr);
    }
    public Task PublishTelescopeCommandAsync(string robotId, TelescopeCommand cmd)
    {
        var corr = Guid.NewGuid().ToString("N");
        var env = new NatsEnvelope<TelescopeCommand> { RobotId = robotId, CorrelationId = corr, Timestamp = DateTimeOffset.UtcNow, Payload = cmd };
        PublishJetStreamAsync(NatsTopics.RobotCmd(robotId, "telescope"), env);
        return Task.FromResult(corr);
    }
    public Task PublishCamToggleCommandAsync(string robotId, CamToggleCommand cmd)
    {
        var corr = Guid.NewGuid().ToString("N");
        var env = new NatsEnvelope<CamToggleCommand> { RobotId = robotId, CorrelationId = corr, Timestamp = DateTimeOffset.UtcNow, Payload = cmd };
        PublishJetStreamAsync(NatsTopics.RobotCmd(robotId, "cam_toggle"), env);
        return Task.FromResult(corr);
    }
    public Task PublishRotateCommandAsync(string robotId, RotateCommand cmd)
    {
        var corr = Guid.NewGuid().ToString("N");
        var env = new NatsEnvelope<RotateCommand> { RobotId = robotId, CorrelationId = corr, Timestamp = DateTimeOffset.UtcNow, Payload = cmd };
        PublishJetStreamAsync(NatsTopics.RobotCmd(robotId, "rotate"), env);
        return Task.FromResult(corr);
    }
    public Task PublishModeCommandAsync(string robotId, ModeCommand cmd)
    {
        var corr = Guid.NewGuid().ToString("N");
        var env = new NatsEnvelope<ModeCommand> { RobotId = robotId, CorrelationId = corr, Timestamp = DateTimeOffset.UtcNow, Payload = cmd };
        PublishJetStreamAsync(NatsTopics.RobotCmd(robotId, "mode"), env);
        return Task.FromResult(corr);
    }
}
