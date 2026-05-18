namespace ServicePlaces.Models;

/// <summary>
/// Structure du message JSON publié par le CapteurSimulé.
/// Exemple : { "placeId": 3, "estOccupee": true, "timestamp": "2024-01-01T12:00:00Z" }
/// </summary>
public class EtatPlaceMessage
{
    public int PlaceId { get; set; }
    public bool EstOccupee { get; set; }
    public DateTime Timestamp { get; set; }
}
