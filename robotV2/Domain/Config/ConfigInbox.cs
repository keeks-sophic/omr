using System.Collections.Generic;

namespace Robot.Domain.Config;

public class ConfigInbox
{
    private readonly Queue<object> _queue = new();
    public void Enqueue(object cfg) => _queue.Enqueue(cfg);
    public object? Dequeue() => _queue.Count > 0 ? _queue.Dequeue() : null;
}

