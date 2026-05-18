namespace ServicePlaces.Models;

public class PlaceEvent
{
    public int PlaceId { get; set; }
    public bool EstOccupee { get; set; }
    public DateTime Timestamp { get; set; }
}