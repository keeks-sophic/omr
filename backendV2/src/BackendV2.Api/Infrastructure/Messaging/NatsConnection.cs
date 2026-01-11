using System;
using NATS.Client;

namespace BackendV2.Api.Infrastructure.Messaging;

public class NatsConnection : IDisposable
{
    private IConnection? _conn;
    public IConnection Get()
    {
        if (_conn != null && _conn.State == ConnState.CONNECTED) return _conn;
        var url = Environment.GetEnvironmentVariable("BACKENDV2_NATS_URL") ?? "nats://localhost:4222";
        var token = Environment.GetEnvironmentVariable("BACKENDV2_NATS_TOKEN");
        var opts = ConnectionFactory.GetDefaultOptions();
        opts.Url = url;
        if (!string.IsNullOrEmpty(token)) opts.Token = token;
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

