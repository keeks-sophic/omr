using System.Collections.Generic;

namespace Robot.Domain.TaskRoute;

public class RouteInbox
{
    private readonly Queue<object> _queue = new();
    public void Enqueue(object routeMsg) => _queue.Enqueue(routeMsg);
    public object? Dequeue() => _queue.Count > 0 ? _queue.Dequeue() : null;
}

