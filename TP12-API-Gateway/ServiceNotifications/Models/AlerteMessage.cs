namespace ServiceNotifications.Models;

public class AlerteMessage
{
    public string Type { get; set; } = string.Empty;
    public double TauxOccupation { get; set; }
    public int PlacesLibres { get; set; }
    public DateTime Timestamp { get; set; }
}
