using ServiceHistorique.Models;
using ServiceHistorique.Projections;

namespace ServiceHistorique.EventStore;

/// <summary>
/// Implémentation in-memory de l'Event Store.
/// Thread-safe via lock — partagé entre le listener RabbitMQ et l'API REST.
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly List<EvenementStocke> _events = new();
    private readonly object _lock = new();
    private long _sequence = 0;
    private readonly Dictionary<int, SnapshotEntry> _snapshots = new();

    public long Count
    {
        get { lock (_lock) return _events.Count; }
    }

    public void Append(string routingKey, EvenementParking evt)
    {
        lock (_lock)
        {
            _sequence++;
            _events.Add(new EvenementStocke
            {
                SequenceId = _sequence,
                RoutingKey = routingKey,
                Type = evt.Type,
                PlaceId = evt.PlaceId,
                Timestamp = evt.Timestamp,
                Message = evt.Message,
                DateIngestion = DateTime.UtcNow
            });
        }
    }

    public IReadOnlyList<EvenementStocke> GetAll()
    {
        lock (_lock) return _events.AsReadOnly();
    }

    public IReadOnlyList<EvenementStocke> GetByPlaceId(int placeId)
    {
        lock (_lock)
            return _events
                .Where(e => e.PlaceId == placeId)
                .OrderBy(e => e.SequenceId)
                .ToList()
                .AsReadOnly();
    }

    public IReadOnlyList<EvenementStocke> GetByType(string type)
    {
        lock (_lock)
            return _events
                .Where(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
    }
    public void SaveSnapshot(int placeId, EtatPlace etat, long sequenceId)
    {
        _snapshots[placeId] = new SnapshotEntry(placeId, etat, sequenceId, DateTime.UtcNow);
    }

    public SnapshotEntry? GetLastSnapshot(int placeId)
    {
        return _snapshots.TryGetValue(placeId, out var snap) ? snap : null;
    }

    public IReadOnlyList<EvenementStocke> ObtenirTous() => _events.AsReadOnly();

    public IReadOnlyList<EvenementStocke> ObtenirParPlace(int placeId) =>
        _events.Where(e => e.PlaceId == placeId).ToList();

public void Reset()
    {
        _snapshots.Clear();
        Console.WriteLine("[🔄] Snapshots vidés — prochain appel rejoue depuis le début");
    }

}