namespace ParkingConsole.Models;

public class Place
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public int Etage { get; set; }
    public bool EstOccupee { get; set; }
    public DateTime DateDerniereMiseAJour { get; set; }
    public string Description { get; set; } = "";
    public Place(int id, int numero, int etage, string description = "")
    {
        Id = id;
        Numero = numero;
        Etage = etage;
        EstOccupee = false;
        DateDerniereMiseAJour = DateTime.Now;
        Description = description;
    }

    // Méthode pour occuper une place
    public void Occuper()
    {
        EstOccupee = true;
        DateDerniereMiseAJour = DateTime.Now;
    }

    // Méthode pour libérer une place
    public void Liberer()
    {
        EstOccupee = false;
        DateDerniereMiseAJour = DateTime.Now;
    }

    public string Afficher()
    {
        string etat = EstOccupee ? "OCCUPÉE" : "LIBRE   ";
        string date = DateDerniereMiseAJour.ToString("dd/MM/yyyy HH:mm:ss");
        return $"Place n°{Numero} | Étage {Etage} | {etat} | Mis à jour : {date}";
    }

    public override string ToString()
    {
        string etat = EstOccupee ? "OCC" : "LIB";
        return $"P{Numero}({etat})";
    }
}
