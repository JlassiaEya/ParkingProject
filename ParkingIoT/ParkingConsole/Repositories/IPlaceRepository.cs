namespace ParkingConsole.Repositories;

using ParkingConsole.Models;

// Interface = contrat. Elle définit ce que DOIT faire un Repository de places.
// Elle ne contient aucune implémentation.
public interface IPlaceRepository
{
    // Récupérer toutes les places
    List<Place> Obtenir();

    // Récupérer une place par son ID
    Place? Obtenir(int id);

    // Ajouter une nouvelle place
    void Ajouter(Place place);

    // Mettre à jour une place existante
    bool MettreAJour(Place place);

    // Supprimer une place par son ID
    bool Supprimer(int id);

    // Compter le nombre total de places
    int Compter();

    // Compter les places libres
    int CompterLibres();

    // Compter les places occupées
    int CompterOccupees();
    //Recherche par numéro de place
    Place? ObtenirParNumero(int numero);
}
