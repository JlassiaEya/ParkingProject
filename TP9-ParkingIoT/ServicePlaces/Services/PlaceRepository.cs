using ServicePlaces.Models;

namespace ServicePlaces.Services;

/// <summary>
/// Repository en mémoire — données partagées entre l'API REST
/// et le service MQTT. Doit être enregistré en Singleton.
/// </summary>
public class PlaceRepository : IPlaceRepository
{
    public AlerteRabbitMqService? AlerteService { get; set; }

    private const double SeuilAlerte = 0.80; // 80%
    private bool _alerteEnvoyee = false;     // eviter le spam d'alertes

    // Verrou pour la concurrence entre le thread MQTT et les requêtes HTTP
    private readonly object _lock = new();
    private readonly List<PlaceEvent> _historique = new();

    private readonly List<Place> _places = new()
    {
        new Place { Id = 1, Numero = "A01", Etage = 0, EstOccupee = false },
        new Place { Id = 2, Numero = "A02", Etage = 0, EstOccupee = false },
        new Place { Id = 3, Numero = "A03", Etage = 0, EstOccupee = false },
        new Place { Id = 4, Numero = "B01", Etage = 1, EstOccupee = false },
        new Place { Id = 5, Numero = "B02", Etage = 1, EstOccupee = false },
    };

    public IEnumerable<Place> GetAll()
    {
        lock (_lock) return _places.ToList();
    }

    public Place? GetById(int id)
    {
        lock (_lock) return _places.FirstOrDefault(p => p.Id == id);
    }


    public IEnumerable<Place> GetLibres()
    {
        lock (_lock) return _places.Where(p => !p.EstOccupee).ToList();
    }
    public (int total, int occupees, int libres, double tauxOccupation, DateTime? derniereMiseAJour) GetStats()
{
    lock (_lock)
    {
        int total = _places.Count;
        int occupees = _places.Count(p => p.EstOccupee);
        int libres = total - occupees;
        double taux = total > 0 ? (occupees * 100.0 / total) : 0;
        
        DateTime? derniereMaj = _places
            .OrderByDescending(p => p.DerniereMiseAJour)
            .FirstOrDefault()?.DerniereMiseAJour;

        return (total, occupees, libres, Math.Round(taux, 2), derniereMaj);
    }
}

    public void UpdateEtat(int placeId, bool estOccupee, DateTime timestamp)
    {
        lock (_lock)
        {
            var place = _places.FirstOrDefault(p => p.Id == placeId);
            if (place is not null)
            {
                place.EstOccupee = estOccupee;
                place.DerniereMiseAJour = timestamp;

                // Enregistrer l'événement dans l'historique
                _historique.Add(new PlaceEvent
                {
                    PlaceId = placeId,
                    EstOccupee = estOccupee,
                    Timestamp = timestamp
                });
            }

            // Verifier le taux d'occupation
            var occupees = _places.Count(p => p.EstOccupee);
            var taux = (double)occupees / _places.Count;

            if (taux >= SeuilAlerte && !_alerteEnvoyee)
            {
                _alerteEnvoyee = true;
                AlerteService?.PublierAlerte(
                    Math.Round(taux * 100, 1),
                    _places.Count - occupees);
            }
            else if (taux < SeuilAlerte)
            {
                _alerteEnvoyee = false; // Reinitialiser quand occupation redescend
            }
        }
    }

    public void AddEvent(PlaceEvent evt)
    {
        lock (_lock)
        {
            _historique.Add(evt);
        }
    }  

    public IEnumerable<PlaceEvent> GetHistorique(int placeId)
    {
        lock (_lock)
        {
            return _historique
                .Where(e => e.PlaceId == placeId)
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }
    }
}
