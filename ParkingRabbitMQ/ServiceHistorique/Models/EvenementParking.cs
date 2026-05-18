namespace ServiceHistorique.Models;

public class EvenementParking
{
    public string Type { get; set; } = string.Empty;
    public int PlaceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Message { get; set; }
}
