namespace DeskNotes.Core.Addons;

public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = [];
            _handlers[type] = list;
        }

        list.Add(handler);
    }

    public void Publish<T>(T message)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            return;

        foreach (var handler in list.ToArray())
        {
            if (handler is Action<T> typed)
                typed(message);
        }
    }

    public void Clear() => _handlers.Clear();
}