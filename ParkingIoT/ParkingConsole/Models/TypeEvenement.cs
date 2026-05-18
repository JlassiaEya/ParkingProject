namespace ParkingConsole.Models;

public enum TypeEvenement
{
    PlaceOccupee,       // Une place vient d'être occupée
    PlaceLiberee,       // Une place vient d'être libérée
    CapteurActive,      // Un capteur a été activé
    CapteurDesactive,   // Un capteur a été désactivé
    AlerteParkingPlein, // Le parking est à capacité maximale
    AlerteQualiteAir,   // La qualité de l'air est mauvaise
    Erreur              // Une erreur est survenue
}
