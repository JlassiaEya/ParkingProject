namespace ServiceHistorique.EventStore;

/// <summary>
/// Enveloppe immuable d'un événement dans l'Event Store.
/// Une fois créé, cet objet ne doit JAMAIS être modifié.
/// </summary>
public sealed class EvenementStocke
{
    // Identifiant unique de l'événement dans le store
    public long SequenceId { get; init; }

    // Routing key RabbitMQ (ex : parking.places.occupee)
    public string RoutingKey { get; init; } = string.Empty;

    // L'événement métier
    public string Type { get; init; } = string.Empty;
    public int PlaceId { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Message { get; init; }

    // Date d'ingestion dans le store (différente du Timestamp de l'événement)
    public DateTime DateIngestion { get; init; } = DateTime.UtcNow;
}

