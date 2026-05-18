namespace ParkingConsole.Models;

public class Evenement
{
    public int Id { get; set; }
    public TypeEvenement Type { get; set; }       // Le type d'événement (enum)
    public DateTime Timestamp { get; set; }       // Quand l'événement a eu lieu
    public string Donnees { get; set; }           // Données supplémentaires (texte libre)
    public int? PlaceId { get; set; }             // Lié à quelle place ? (nullable)
    public int? CapteurId { get; set; }           // Lié à quel capteur ? (nullable)

    public Evenement(int id, TypeEvenement type, string donnees, int? placeId = null, int? capteurId = null)
    {
        Id = id;
        Type = type;
        Timestamp = DateTime.Now;
        Donnees = donnees;
        PlaceId = placeId;
        CapteurId = capteurId;
    }

    public string Afficher()
    {
        string date = Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
        string place = PlaceId.HasValue ? $"Place {PlaceId}" : "—";
        string capteur = CapteurId.HasValue ? $"Capteur {CapteurId}" : "—";
        return $"[{date}] {Type,-20} | Place : {place,-10} | Capteur : {capteur,-12} | {Donnees}";
    }

    public override string ToString()
    {
        return $"E{Id}({Type})";
    }
}
