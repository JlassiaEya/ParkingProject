using ServicePlaces.Models;

public interface IPlaceRepository
{
    IEnumerable<Place> GetAll();
    Place? GetById(int id);
    void UpdateEtat(int placeId, bool estOccupee, DateTime timestamp);
    IEnumerable<Place> GetLibres();

    // Bonus3
    (int total, int occupees, int libres, double tauxOccupation, DateTime? derniereMiseAJour) GetStats();

    //Bonus 4

    void AddEvent(PlaceEvent evt);
    IEnumerable<PlaceEvent> GetHistorique(int placeId);
}
