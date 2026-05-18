namespace ParkingConsole.Repositories;

using ParkingConsole.Models;

public interface IEvenementRepository
{
    List<Evenement> Obtenir();
    Evenement? Obtenir(int id);
    List<Evenement> ObtenirParType(TypeEvenement type);
    List<Evenement> ObtenirParPlace(int placeId);
    List<Evenement> ObtenirDernier(int nombre);
    void Ajouter(Evenement evenement);
    int ProchainId();
}
