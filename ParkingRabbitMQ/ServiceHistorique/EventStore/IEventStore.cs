using ServiceHistorique.Models;
using ServiceHistorique.Projections;
namespace ServiceHistorique.EventStore;

public interface IEventStore
{
    // Ajouter un événement (append-only — jamais de modification)
    void Append(string routingKey, EvenementParking evt);

    // Lire tous les événements
    IReadOnlyList<EvenementStocke> GetAll();

    // Lire les événements d'une place spécifique
    IReadOnlyList<EvenementStocke> GetByPlaceId(int placeId);

    // Lire les événements par type
    IReadOnlyList<EvenementStocke> GetByType(string type);

    // Nombre total d'événements stockés
    long Count { get; }
    // Nouvelles méthodes snapshot
    void SaveSnapshot(int placeId, EtatPlace etat, long sequenceId);
    SnapshotEntry GetLastSnapshot(int placeId);
    void Reset(); 
}

