using ServiceHistorique.EventStore;
using ServiceHistorique.Projections;

namespace ServiceHistorique.Projections;

public class PlaceProjection : IPlaceProjection
{
    private readonly IEventStore _store;
    private static readonly int[] PLACE_IDS = { 1, 2, 3, 4, 5 };

    public PlaceProjection(IEventStore store)
    {
        _store = store;
    }

    public IReadOnlyList<EtatPlace> ProjecterTout()
    {
        return PLACE_IDS
            .Select(id => ProjecterPlace(id))
            .Where(e => e is not null)
            .Cast<EtatPlace>()
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Rejoue tous les événements d'une place pour reconstruire son état.
    /// Si 'jusqu_au' est fourni, rejoue uniquement jusqu'à cet instant.
    /// </summary>
    public EtatPlace? ProjecterPlace(int placeId, DateTime? jusqu_au = null)
    {
        var evenements = _store.GetByPlaceId(placeId);

        // Filtrer par date si demandé (time-travel query)
        if (jusqu_au.HasValue)
            evenements = evenements
                .Where(e => e.Timestamp <= jusqu_au.Value)
                .ToList()
                .AsReadOnly();

        if (!evenements.Any())
            return new EtatPlace(placeId, false, DateTime.MinValue, 0, 0);

        // ─── REJEU ────────────────────────────────────────────────
        // On applique chaque événement dans l'ordre chronologique.
        // C'est exactement ce que fait un moteur Event Sourcing.
        bool estOccupee = false;
        int occupations = 0;
        int liberations = 0;
        DateTime derniere = DateTime.MinValue;

        foreach (var evt in evenements.OrderBy(e => e.SequenceId))
        {
            switch (evt.Type)
            {
                case "PlaceOccupee":
                    estOccupee = true;
                    occupations++;
                    derniere = evt.Timestamp;
                    break;
                case "PlaceLibree":
                    estOccupee = false;
                    liberations++;
                    derniere = evt.Timestamp;
                    break;
            }
        }

        return new EtatPlace(placeId, estOccupee, derniere, occupations, liberations);
    }
}