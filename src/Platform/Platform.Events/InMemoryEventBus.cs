namespace Platform.Events;

/// <summary>
/// Reference implementation of <see cref="IEventBus"/> — an in-process pub/sub bus. Proves the
/// publish/subscribe contract now; a real deployment swaps this for a RabbitMQ-backed (or Azure Service
/// Bus/Kafka) implementation behind the same interface (ADR-5) without any subscriber code changing.
/// Publishing awaits all matching handlers — a real out-of-process bus has no such synchronous guarantee
/// (consumers process independently, possibly on different services), so this is a deliberate
/// simplification for a single-process reference implementation, not a contract callers should rely on.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<Func<IntegrationEvent, CancellationToken, Task>>> _handlersByEventType = new();

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        List<Func<IntegrationEvent, CancellationToken, Task>> handlers;
        lock (_lock)
        {
            handlers = _handlersByEventType.TryGetValue(integrationEvent.EventType, out var registered)
                ? new List<Func<IntegrationEvent, CancellationToken, Task>>(registered)
                : new List<Func<IntegrationEvent, CancellationToken, Task>>();
        }

        foreach (var handler in handlers)
        {
            await handler(integrationEvent, cancellationToken);
        }
    }

    public IDisposable Subscribe(string eventType, Func<IntegrationEvent, CancellationToken, Task> handler)
    {
        lock (_lock)
        {
            if (!_handlersByEventType.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Func<IntegrationEvent, CancellationToken, Task>>();
                _handlersByEventType[eventType] = handlers;
            }

            handlers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_handlersByEventType.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                }
            }
        });
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public Subscription(Action onDispose) => _onDispose = onDispose;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _onDispose();
        }
    }
}
