namespace ServiceHistorique.Models;

public class EvenementHistorique
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PlaceId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public bool EstOccupee { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty; 
}