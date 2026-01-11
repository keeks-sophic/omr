using System;
using System.Collections.Generic;

namespace Robot.Services;

public enum EndpointRole
{
    Subscribe,
    Publish
}

public record EndpointRegistration(string Subject, EndpointRole Role);

public class NatsService
{
    private readonly HashSet<string> _subscriptions = new();
    private readonly HashSet<string> _publications = new();
    private bool _connected;
    public IReadOnlyCollection<string> RegisteredSubscriptions => _subscriptions;
    public IReadOnlyCollection<string> RegisteredPublications => _publications;
    public bool IsConnected => _connected;
    public bool Connect(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        _connected = true;
        return true;
    }
    public bool TryConnect(string? url, int maxRetries = 3, int initialBackoffMs = 200)
    {
        var delay = initialBackoffMs;
        for (var i = 0; i < maxRetries; i++)
        {
            if (Connect(url)) return true;
            System.Threading.Thread.Sleep(delay);
            delay = Math.Min(delay * 2, 2000);
        }
        return false;
    }
    public void Subscribe(string subject) => _subscriptions.Add(subject);
    public void Publish(string subject) => _publications.Add(subject);
}
