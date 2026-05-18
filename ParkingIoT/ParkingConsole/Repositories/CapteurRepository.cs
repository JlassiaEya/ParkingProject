namespace ParkingConsole.Repositories;

using ParkingConsole.Models;

public class CapteurRepository : ICapteurRepository
{
    private readonly List<Capteur> _capteurs;

    public CapteurRepository()
    {
        _capteurs = new List<Capteur>
        {
            new Capteur(1, TypeCapteur.Presence, 1),
            new Capteur(2, TypeCapteur.Presence, 2),
            new Capteur(3, TypeCapteur.Presence, 3),
            new Capteur(4, TypeCapteur.Presence, 4),
            new Capteur(5, TypeCapteur.Presence, 5),
            new Capteur(6, TypeCapteur.Presence, 6),
            new Capteur(7, TypeCapteur.Presence, 7),
            new Capteur(8, TypeCapteur.Presence, 8),
            new Capteur(9, TypeCapteur.Presence, 9),
            new Capteur(10, TypeCapteur.Presence, 10),
            new Capteur(11, TypeCapteur.Presence, 11),
            new Capteur(12, TypeCapteur.Presence, 12),
            new Capteur(13, TypeCapteur.CO2, -1),          // Capteur CO₂ global (pas lié à une place)
            new Capteur(14, TypeCapteur.Temperature, -1),  // Capteur température global
};
    }

    public List<Capteur> Obtenir()
    {
        return _capteurs;
    }

    public Capteur? Obtenir(int id)
    {
        foreach (Capteur capteur in _capteurs)
        {
            if (capteur.Id == id)
                return capteur;
        }
        return null;
    }

    public List<Capteur> ObtenirParPlace(int placeId)
    {
        List<Capteur> result = new List<Capteur>();
        foreach (Capteur capteur in _capteurs)
        {
            if (capteur.PlaceId == placeId)
                result.Add(capteur);
        }
        return result;
    }

    public List<Capteur> ObtenirParType(TypeCapteur type)
    {
        List<Capteur> result = new List<Capteur>();
        foreach (Capteur capteur in _capteurs)
        {
            if (capteur.Type == type)
                result.Add(capteur);
        }
        return result;
    }

    public void Ajouter(Capteur capteur)
    {
        _capteurs.Add(capteur);
    }

    public bool MettreAJour(Capteur capteur)
    {
        for (int i = 0; i < _capteurs.Count; i++)
        {
            if (_capteurs[i].Id == capteur.Id)
            {
                _capteurs[i] = capteur;
                return true;
            }
        }
        return false;
    }

    public bool Supprimer(int id)
    {
        for (int i = 0; i < _capteurs.Count; i++)
        {
            if (_capteurs[i].Id == id)
            {
                _capteurs.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}
