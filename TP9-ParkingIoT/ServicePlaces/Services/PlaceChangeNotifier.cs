using System.Threading.Channels;

namespace ServicePlaces.Services;

// Singleton qui notifie tous les clients gRPC connectés
public class PlaceChangeNotifier
{
    private readonly List<ChannelWriter<PlaceUpdate>> _subscribers = new();
    private readonly object _lock = new();

    public void Subscribe(ChannelWriter<PlaceUpdate> writer)
    {
        lock (_lock) _subscribers.Add(writer);
    }

    public void Unsubscribe(ChannelWriter<PlaceUpdate> writer)
    {
        lock (_lock) _subscribers.Remove(writer);
        writer.TryComplete();
    }

    public void Notify(PlaceUpdate update)
    {
        lock (_lock)
            foreach (var writer in _subscribers)
                writer.TryWrite(update);
    }
}