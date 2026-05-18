namespace ParkingConsole.Models;

// DTO = Data Transfer Object
// Un objet utilisé uniquement pour transférer des données entre les couches
public class ResumeParkingDto
{
    public int TotalPlaces { get; set; }
    public int PlacesLibres { get; set; }
    public int PlacesOccupees { get; set; }
    public double TauxOccupation { get; set; }     // En pourcentage
    public bool EstPlein { get; set; }
    public bool EstVide { get; set; }
}