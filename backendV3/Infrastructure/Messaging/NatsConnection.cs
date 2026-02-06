using NATS.Client;

namespace BackendV3.Infrastructure.Messaging;

public sealed class NatsConnection : IDisposable
{
    private IConnection? _conn;

    public IConnection Get()
    {
        if (_conn != null && _conn.State == ConnState.CONNECTED) return _conn;

        var url = Environment.GetEnvironmentVariable("BACKENDV3_NATS_URL") ?? "nats://localhost:4222";
        var token = Environment.GetEnvironmentVariable("BACKENDV3_NATS_TOKEN");

        var opts = ConnectionFactory.GetDefaultOptions();
        opts.Url = url;
        if (!string.IsNullOrWhiteSpace(token)) opts.Token = token;

        _conn = new ConnectionFactory().CreateConnection(opts);
        return _conn;
    }

    public void Dispose()
    {
        _conn?.Drain();
        _conn?.Close();
        _conn = null;
    }
}

