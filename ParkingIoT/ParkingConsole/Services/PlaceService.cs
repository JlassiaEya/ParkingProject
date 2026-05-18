namespace ParkingConsole.Services;

using ParkingConsole.Models;
using ParkingConsole.Repositories;

public class PlaceService : IPlaceService
{
    // Le service reçoit ses dépendances via le constructeur
    // Il ne crée PAS lui-même le Repository → c'est l'injection de dépendances
    private readonly IPlaceRepository _placeRepository;
    private readonly IEvenementRepository _evenementRepository;

    public PlaceService(IPlaceRepository placeRepository, IEvenementRepository evenementRepository)
    {
        _placeRepository = placeRepository;
        _evenementRepository = evenementRepository;
    }

    public List<Place> Obtenir()
    {
        return _placeRepository.Obtenir();
    }

    public Place? Obtenir(int id)
    {
        return _placeRepository.Obtenir(id);
    }

    public bool OccuperPlace(int placeId)
    {
        // Étape 1 : Chercher la place
        Place? place = _placeRepository.Obtenir(placeId);

        if (place == null)
        {
            Console.WriteLine($"  ⚠️  Place avec l'ID {placeId} non trouvée.");
            return false;
        }

        // Étape 2 : Vérifier qu'elle est libre
        if (place.EstOccupee)
        {
            Console.WriteLine($"  ⚠️  La place n°{place.Numero} est déjà occupée.");
            return false;
        }

        // Étape 3 : Occuper la place
        place.Occuper();

        // Étape 4 : Enregistrer l'événement
        Evenement evenement = new Evenement(
            _evenementRepository.ProchainId(),
            TypeEvenement.PlaceOccupee,
            $"La place n°{place.Numero} (étage {place.Etage}) a été occupée.",
            placeId: place.Id
        );
        _evenementRepository.Ajouter(evenement);

        // Étape 5 : Vérifier si le parking est plein
        if (_placeRepository.CompterLibres() == 0)
        {
            Evenement alertePlein = new Evenement(
                _evenementRepository.ProchainId(),
                TypeEvenement.AlerteParkingPlein,
                "⚠️  Le parking est complet ! Plus de places disponibles."
            );
            _evenementRepository.Ajouter(alertePlein);
            Console.WriteLine("  🚨 ALERTE : Le parking est plein !");
        }

        return true;
    }

    public bool LibererPlace(int placeId)
    {
        Place? place = _placeRepository.Obtenir(placeId);

        if (place == null)
        {
            Console.WriteLine($"  ⚠️  Place avec l'ID {placeId} non trouvée.");
            return false;
        }

        if (!place.EstOccupee)
        {
            Console.WriteLine($"  ⚠️  La place n°{place.Numero} est déjà libre.");
            return false;
        }

        place.Liberer();

        Evenement evenement = new Evenement(
            _evenementRepository.ProchainId(),
            TypeEvenement.PlaceLiberee,
            $"La place n°{place.Numero} (étage {place.Etage}) a été libérée.",
            placeId: place.Id
        );
        _evenementRepository.Ajouter(evenement);

        return true;
    }

    public ResumeParkingDto ObteniRresume()
    {
        int total = _placeRepository.Compter();
        int libres = _placeRepository.CompterLibres();
        int occupees = _placeRepository.CompterOccupees();

        return new ResumeParkingDto
        {
            TotalPlaces = total,
            PlacesLibres = libres,
            PlacesOccupees = occupees,
            TauxOccupation = total > 0 ? (occupees * 100.0) / total : 0,
            EstPlein = libres == 0,
            EstVide = occupees == 0
        };
    }

    public List<Place> ObteniePlacesLibres()
    {
        List<Place> result = new List<Place>();
        foreach (Place place in _placeRepository.Obtenir())
        {
            if (!place.EstOccupee)
                result.Add(place);
        }
        return result;
    }

    public List<Place> ObtenirParEtage(int etage)
    {
        List<Place> result = new List<Place>();
        foreach (Place place in _placeRepository.Obtenir())
        {
            if (place.Etage == etage)
                result.Add(place);
        }
        return result;
    }

    public void Ajouter(Place place)
    {
        place.DateDerniereMiseAJour = DateTime.Now;

         _placeRepository.Ajouter(place);
    }

    public bool Supprimer(int id)
    {
        Place? place = _placeRepository.Obtenir(id);

        if (place == null)
            return false;

        return _placeRepository.Supprimer(id);
    }
}
