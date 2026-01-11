using System;
using System.IO;
using System.Text.Json;
using Robot.Options;
using Robot.Services;
using Robot.Workers;
using Robot.Domain.Identity;
using Robot.Domain.State;
using Robot.Domain.Telemetry;
using Robot.Options;

var appSettingsPath1 = Path.Combine(Environment.CurrentDirectory, "appsettings.json");
var appSettingsPath2 = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
var appSettingsPath = File.Exists(appSettingsPath1) ? appSettingsPath1 : appSettingsPath2;
RobotOptions robotOptions = new();
NatsOptions natsOptions = new();
TickOptions tickOptions = new();
if (File.Exists(appSettingsPath))
{
    using var stream = File.OpenRead(appSettingsPath);
    using var doc = JsonDocument.Parse(stream);
    if (doc.RootElement.TryGetProperty("Robot", out var robotEl))
    {
        robotOptions.Id = robotEl.TryGetProperty("Id", out var v1) ? v1.GetString() : null;
        robotOptions.Name = robotEl.TryGetProperty("Name", out var v2) ? v2.GetString() : null;
        robotOptions.Ip = robotEl.TryGetProperty("Ip", out var v3) ? v3.GetString() : null;
    }
    if (doc.RootElement.TryGetProperty("Nats", out var natsEl))
    {
        natsOptions.Url = natsEl.TryGetProperty("Url", out var u) ? u.GetString() : null;
        natsOptions.Stream = natsEl.TryGetProperty("Stream", out var s) ? s.GetString() : null;
        natsOptions.Consumer = natsEl.TryGetProperty("Consumer", out var c) ? c.GetString() : null;
    }
    if (doc.RootElement.TryGetProperty("Tick", out var tickEl))
    {
        tickOptions.HeartbeatSeconds = tickEl.TryGetProperty("HeartbeatSeconds", out var h) ? h.GetInt32() : tickOptions.HeartbeatSeconds;
        tickOptions.SnapshotSeconds = tickEl.TryGetProperty("SnapshotSeconds", out var ss) ? ss.GetInt32() : tickOptions.SnapshotSeconds;
        tickOptions.MotionMs = tickEl.TryGetProperty("MotionMs", out var mm) ? mm.GetInt32() : tickOptions.MotionMs;
        tickOptions.RouteSeconds = tickEl.TryGetProperty("RouteSeconds", out var rs) ? rs.GetInt32() : tickOptions.RouteSeconds;
        tickOptions.BatterySeconds = tickEl.TryGetProperty("BatterySeconds", out var bs) ? bs.GetInt32() : tickOptions.BatterySeconds;
        tickOptions.HealthSeconds = tickEl.TryGetProperty("HealthSeconds", out var hs) ? hs.GetInt32() : tickOptions.HealthSeconds;
    }
}
string? idOverride = null;
string? nameOverride = null;
string? ipOverride = null;
foreach (var arg in args)
{
    if (arg.StartsWith("--id=")) idOverride = arg.Substring(5);
    else if (arg.StartsWith("--name=")) nameOverride = arg.Substring(7);
    else if (arg.StartsWith("--ip=")) ipOverride = arg.Substring(5);
}
var identityService = new IdentityService();
var identity = identityService.Resolve(robotOptions, idOverride, nameOverride, ipOverride);
if (string.IsNullOrWhiteSpace(identity.Id))
{
    Console.Error.WriteLine("robotId is required; startup aborted.");
    Environment.Exit(2);
}
var nats = new NatsService();
var telemetry = new TelemetryService(nats);
var traffic = new Robot.Domain.Traffic.TrafficAdapter();
var cmdInbox = new Robot.Domain.Commands.CommandInbox();
var taskInbox = new Robot.Domain.TaskRoute.TaskInbox();
var routeInbox = new Robot.Domain.TaskRoute.RouteInbox();
var cfgInbox = new Robot.Domain.Config.ConfigInbox();
var store = new RobotStateStore();
store.State.Identity = identity;
var snapshotWorker = new StateSnapshotWorker(nats, store);
var commands = new CommandListenerService(nats, telemetry, store, cmdInbox, taskInbox, routeInbox, cfgInbox, traffic, snapshotWorker);
var worker = new RobotWorker(commands, telemetry);
var connected = nats.TryConnect(natsOptions.Url);
if (connected)
{
    worker.Start(identity.Id);
    var presence = new PresencePublisher(nats);
    presence.PublishHello(identity.Id);
    var heartbeat = new HeartbeatWorker(nats);
    heartbeat.SendHeartbeat(identity.Id, 0, null);
    snapshotWorker.PublishSnapshot(identity.Id);
    var drive = new Robot.Domain.Hardware.Drivers.DriveDriver();
    var motionController = new Robot.Domain.Motion.MotionController(store, drive, snapshotWorker, telemetry, tickSeconds: tickOptions.MotionMs / 1000.0);
    var motionTick = new MotionTickWorker(store, traffic, snapshotWorker, motionController);
    motionTick.Tick(identity.Id);
    var routeProgress = new Robot.Domain.TaskRoute.RouteProgressTracker();
    var routeExecutor = new Robot.Domain.TaskRoute.TaskRouteExecutor(store, telemetry, routeProgress);
    var scheduler = new PeriodicScheduler(heartbeat, snapshotWorker, motionTick, routeExecutor, telemetry, tickOptions);
    scheduler.Start(identity.Id);
}
else
{
    Console.Error.WriteLine("NATS connect failed; robot remains disconnected.");
}
Console.WriteLine("Subscriptions:");
foreach (var s in nats.RegisteredSubscriptions) Console.WriteLine(s);
Console.WriteLine("Publications:");
foreach (var p in nats.RegisteredPublications) Console.WriteLine(p);
