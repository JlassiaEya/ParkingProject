namespace ServicePlaces.Models;

public class Place
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int Etage { get; set; }
    public bool EstOccupee { get; set; }
    public DateTime DerniereMiseAJour { get; set; } = DateTime.UtcNow;
}
