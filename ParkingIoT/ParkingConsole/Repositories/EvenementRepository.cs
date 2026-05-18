namespace ParkingConsole.Repositories;

using ParkingConsole.Models;

public class EvenementRepository : IEvenementRepository
{
    private readonly List<Evenement> _evenements;
    private int _prochainId;

    public EvenementRepository()
    {
        _evenements = new List<Evenement>();
        _prochainId = 1;
    }

    public List<Evenement> Obtenir()
    {
        return _evenements;
    }

    public Evenement? Obtenir(int id)
{
        foreach (Evenement evenement in _evenements)
        {
            if (evenement.Id == id)
                return evenement;
        }
        return null;
    }

    public List<Evenement> ObtenirParType(TypeEvenement type)
    {
        List<Evenement> result = new List<Evenement>();
        foreach (Evenement evenement in _evenements)
        {
            if (evenement.Type == type)
                result.Add(evenement);
        }
        return result;
    }

    public List<Evenement> ObtenirParPlace(int placeId)
    {
        List<Evenement> result = new List<Evenement>();
        foreach (Evenement evenement in _evenements)
        {
            if (evenement.PlaceId == placeId)
                result.Add(evenement);
        }
        return result;
    }

    // Retourne les N derniers événements
    public List<Evenement> ObtenirDernier(int nombre)
    {
        List<Evenement> result = new List<Evenement>();
        int startIndex = _evenements.Count - nombre;
        if (startIndex < 0) startIndex = 0;

        for (int i = startIndex; i < _evenements.Count; i++)
        {
            result.Add(_evenements[i]);
        }
        return result;
    }

    public void Ajouter(Evenement evenement)
    {
        _evenements.Add(evenement);
        _prochainId++;
    }

    public int ProchainId()
    {
        return _prochainId;
    }
}
