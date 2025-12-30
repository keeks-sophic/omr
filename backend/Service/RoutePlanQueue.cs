using System.Threading.Channels;

namespace Backend.Service;

public class RoutePlanTask
{
    public string Ip { get; set; } = string.Empty;
    public int MapId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public interface IRoutePlanQueue
{
    ValueTask EnqueueAsync(RoutePlanTask task, CancellationToken ct);
    IAsyncEnumerable<RoutePlanTask> ReadAllAsync(CancellationToken ct);
}

public class RoutePlanQueue : IRoutePlanQueue
{
    private readonly Channel<RoutePlanTask> _channel = Channel.CreateUnbounded<RoutePlanTask>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(RoutePlanTask task, CancellationToken ct) => _channel.Writer.WriteAsync(task, ct);

    public IAsyncEnumerable<RoutePlanTask> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

