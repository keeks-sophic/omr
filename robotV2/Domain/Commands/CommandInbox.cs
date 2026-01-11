using System.Collections.Generic;

namespace Robot.Domain.Commands;

public class CommandInbox
{
    private readonly Queue<object> _queue = new();
    public void Enqueue(object cmd) => _queue.Enqueue(cmd);
    public object? Dequeue() => _queue.Count > 0 ? _queue.Dequeue() : null;
}
