using System.Collections.Generic;

namespace Robot.Domain.TaskRoute;

public class TaskInbox
{
    private readonly Queue<object> _queue = new();
    public void Enqueue(object task) => _queue.Enqueue(task);
    public object? Dequeue() => _queue.Count > 0 ? _queue.Dequeue() : null;
}

