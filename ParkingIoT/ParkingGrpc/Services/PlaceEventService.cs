using System.Collections.Concurrent;
using System.Threading.Channels;
using ParkingGrpc.Protos;

namespace ParkingGrpc.Services;

/// <summary>
/// Service pour gérer les événements de changement d'état des places
/// </summary>
public class PlaceEventService
{
    private readonly ConcurrentDictionary<string, Channel<PlaceUpdate>> _subscribers;
    private readonly ILogger<PlaceEventService> _logger;

    public PlaceEventService(ILogger<PlaceEventService> logger)
    {
        _subscribers = new ConcurrentDictionary<string, Channel<PlaceUpdate>>();
        _logger = logger;
    }

    /// <summary>
    /// S'abonner aux événements de places
    /// </summary>
    public Channel<PlaceUpdate> Subscribe(string subscriberId)
    {
        var channel = Channel.CreateUnbounded<PlaceUpdate>();
        _subscribers.TryAdd(subscriberId, channel);

        _logger.LogInformation("Nouveau subscriber : {SubscriberId}", subscriberId);

        return channel;
    }

    /// <summary>
    /// Se désabonner des événements
    /// </summary>
    public void Unsubscribe(string subscriberId)
    {
        if (_subscribers.TryRemove(subscriberId, out var channel))
        {
            channel.Writer.Complete();
            _logger.LogInformation("Subscriber désinscrit : {SubscriberId}", subscriberId);
        }
    }

    /// <summary>
    /// Publier un événement à tous les subscribers
    /// </summary>
    public async Task PublishAsync(PlaceUpdate update)
    {
        _logger.LogInformation(
            "Publication événement : {EventType} pour place {PlaceId}",
            update.EventType,
            update.Place.Id
        );

        foreach (var subscriber in _subscribers.Values)
        {
            await subscriber.Writer.WriteAsync(update);
        }
    }

    /// <summary>
    /// Nombre de subscribers actifs
    /// </summary>
    public int SubscriberCount => _subscribers.Count;
}
