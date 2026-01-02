using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NATS.Client;
using NATS.Client.JetStream;

namespace Backend.Service;

public class NatsService
{
    private readonly ILogger<NatsService> _logger;
    private IConnection? _conn;
    private IJetStream? _js;
    private IJetStreamManagement? _jm;

    public NatsService(ILogger<NatsService> logger)
    {
        _logger = logger;
    }

    public Task ConnectAsync(string url, CancellationToken token)
    {
        if (_conn != null) return Task.CompletedTask;
        var opts = ConnectionFactory.GetDefaultOptions();
        opts.Url = url;
        _conn = new ConnectionFactory().CreateConnection(opts);
        try
        {
            _js = _conn.CreateJetStreamContext();
            _jm = _conn.CreateJetStreamManagementContext();
        }
        catch { }
        _logger.LogInformation("Connected to NATS: {Url}", url);
        return Task.CompletedTask;
    }

    public Task EnsureStreamAsync(string streamName, params string[] subjects)
    {
        if (_jm == null || string.IsNullOrWhiteSpace(streamName)) return Task.CompletedTask;
        try
        {
            var info = _jm.GetStreamInfo(streamName);
            var existing = (info?.Config?.Subjects ?? new List<string>()).ToArray();
            var required = subjects ?? Array.Empty<string>();
            var needUpdate = required.Any(s => !existing.Contains(s)) || existing.Length != required.Length;
            if (needUpdate)
            {
                try
                {
                    var cfg = StreamConfiguration.Builder()
                        .WithName(streamName)
                        .WithSubjects(required)
                        .WithStorageType(StorageType.File)
                        .Build();
                    _jm.UpdateStream(cfg);
                    _logger.LogInformation("JetStream stream updated: {Stream} → {Subjects}", streamName, string.Join(",", required));
                }
                catch
                {
                    _jm.DeleteStream(streamName);
                    var cfg = StreamConfiguration.Builder()
                        .WithName(streamName)
                        .WithSubjects(required)
                        .WithStorageType(StorageType.File)
                        .Build();
                    _jm.AddStream(cfg);
                    _logger.LogInformation("JetStream stream recreated: {Stream} → {Subjects}", streamName, string.Join(",", required));
                }
            }
        }
        catch
        {
            var cfg = StreamConfiguration.Builder()
                .WithName(streamName)
                .WithSubjects(subjects)
                .WithStorageType(StorageType.File)
                .Build();
            _jm.AddStream(cfg);
            _logger.LogInformation("JetStream stream ensured: {Stream} → {Subjects}", streamName, string.Join(",", subjects));
        }
        return Task.CompletedTask;
    }

    public Task PublishAsync(string subject, object payload, CancellationToken token)
    {
        if (_conn == null) return Task.CompletedTask;
        var json = JsonSerializer.Serialize(payload);
        var data = Encoding.UTF8.GetBytes(json);
        if (_js != null)
        {
            try { _js.Publish(subject, data); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JetStream publish failed {Subject}", subject);
                try { _conn?.Publish(subject, data); } catch { }
            }
        }
        else
        {
            _conn.Publish(subject, data);
        }
        return Task.CompletedTask;
    }

    public Task PublishCoreAsync(string subject, object payload, CancellationToken token)
    {
        if (_conn == null) return Task.CompletedTask;
        var json = JsonSerializer.Serialize(payload);
        var data = Encoding.UTF8.GetBytes(json);
        _conn.Publish(subject, data);
        return Task.CompletedTask;
    }

    public IAsyncSubscription Subscribe(string subject, EventHandler<MsgHandlerEventArgs> handler)
    {
        if (_conn == null) throw new InvalidOperationException("NATS not connected");
        return _conn.SubscribeAsync(subject, handler);
    }
}
