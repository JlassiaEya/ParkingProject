namespace ParkingConsole.Repositories;

using ParkingConsole.Models;

// Implémentation concrète du Repository.
// Elle stocke les données en mémoire dans une List<Place>.
public class PlaceRepository : IPlaceRepository
{
    // La liste en mémoire qui simule une base de données
    private readonly List<Place> _places;
    private int _nextId;

    // Le constructeur initialise la liste avec des données par défaut
    public PlaceRepository()
    {
        _places = new List<Place>
        {
            new Place(1, 101, 1, "Côté entrée"),
            new Place(2, 102, 1, "Côté sortie"),
            new Place(3, 103, 1, ""),
            new Place(4, 104, 1, "Côté fenêtre"),
            new Place(5, 201, 2, "Côté ascenseur"),
            new Place(6, 202, 2, ""),
            new Place(7, 203, 2, "Réservée handicap"),
            new Place(8, 204, 2, "Côté escalier"),
            new Place(9, 301, 3, ""),
            new Place(10, 302, 3, "Côté terrasse"),
            new Place(11, 303, 3, ""),
            new Place(12, 304, 3, "Côté fenêtre"),
        };
    }

    public List<Place> Obtenir()
    {
        return _places;
    }

    public Place? Obtenir(int id)
    {
        // Chercher la place avec l'ID correspondant
        foreach (Place place in _places)
        {
            if (place.Id == id)
                return place;
        }
        return null;  // Rien trouvé
    }

    public void Ajouter(Place place)
    {
       _nextId = _places.Max(p => p.Id) + 1;
        place.Id = _nextId++;
        _places.Add(place);
    }

    public bool MettreAJour(Place place)
    {
        for (int i = 0; i < _places.Count; i++)
        {
            if (_places[i].Id == place.Id)
            {
                _places[i] = place;  // Remplacer la place existante
                return true;
            }
        }
        return false;  // Place non trouvée
    }

    public bool Supprimer(int id)
    {
        for (int i = 0; i < _places.Count; i++)
        {
            if (_places[i].Id == id)
            {
                _places.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public int Compter()
    {
        return _places.Count;
    }

    public int CompterLibres()
    {
        int count = 0;
        foreach (Place place in _places)
        {
            if (!place.EstOccupee)
                count++;
        }
        return count;
    }

    public int CompterOccupees()
    {
        int count = 0;
        foreach (Place place in _places)
        {
            if (place.EstOccupee)
                count++;
        }
        return count;
    }
    public Place? ObtenirParNumero(int numero)
    {
        foreach (Place place in _places)
        {
            if (place.Numero == numero)
                return place;
        }
        return null;
    }
}
