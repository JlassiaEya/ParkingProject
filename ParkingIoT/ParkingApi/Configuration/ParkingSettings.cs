namespace ParkingApi.Configuration;

/// <summary>
/// Configuration du parking
/// </summary>
public class ParkingSettings
{
    public const string SectionName = "ParkingConfiguration";

    public int NombreMaxPlaces { get; set; } = 100;
    public int NombreEtages { get; set; } = 3;
    public int SeuilAlerteOccupation { get; set; } = 80;
    public bool ActiverNotifications { get; set; } = true;
    public string NomParking { get; set; } = "Parking Intelligent";
}
