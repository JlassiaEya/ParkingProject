namespace ParkingConsole.Services;

using ParkingConsole.Models;

// Interface du service métier pour les places
// Elle définit les opérations MÉTIER disponibles
public interface IPlaceService
{
    // Récupérer toutes les places
    List<Place> Obtenir();

    // Récupérer une place par ID
    Place? Obtenir(int id);

    // Occuper une place (logique métier : vérifie que la place existe et est libre)
    bool OccuperPlace(int placeId);

    // Libérer une place (logique métier : vérifie que la place existe et est occupée)
    bool LibererPlace(int placeId);

    // Obtenir un résumé du parking
    ResumeParkingDto ObteniRresume();

    // Obtenir les places libres
    List<Place> ObteniePlacesLibres();

    // Obtenir les places par étage
    List<Place> ObtenirParEtage(int etage);
    void Ajouter(Place place);
    bool Supprimer(int id);
}
