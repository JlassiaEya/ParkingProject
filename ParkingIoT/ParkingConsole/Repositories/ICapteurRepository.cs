namespace ParkingConsole.Repositories;

using ParkingConsole.Models;

public interface ICapteurRepository
{
    List<Capteur> Obtenir();
    Capteur? Obtenir(int id);
    List<Capteur> ObtenirParPlace(int placeId);
    List<Capteur> ObtenirParType(TypeCapteur type);
    void Ajouter(Capteur capteur);
    bool MettreAJour(Capteur capteur);
    bool Supprimer(int id);
}
