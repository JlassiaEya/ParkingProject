using ParkingConsole.Models;

public class Capteur
{
    public int Id { get; set; }
    public TypeCapteur Type { get; set; }          // Utilise l'enum TypeCapteur
    public int PlaceId { get; set; }               // Liée à quelle place (-1 si global)
    public bool EstActif { get; set; }
    public double DerniereValeur { get; set; }
    public DateTime DateDernieremesure { get; set; }

    public Capteur(int id, TypeCapteur type, int placeId)
    {
        Id = id;
        Type = type;
        PlaceId = placeId;
        EstActif = true;
        DerniereValeur = 0;
        DateDernieremesure = DateTime.Now;
    }

    // Mettre à jour la dernière valeur mesurée
    public void MettreAJourValeur(double valeur)
    {
        DerniereValeur = valeur;
        DateDernieremesure = DateTime.Now;
    }

    // Activer le capteur
    public void Activer()
    {
        EstActif = true;
    }

    // Désactiver le capteur
    public void Desactiver()
    {
        EstActif = false;
    }

    public string Afficher()
    {
        string etat = EstActif ? "ACTIF  " : "INACTIF";
        string date = DateDernieremesure.ToString("dd/MM/yyyy HH:mm:ss");
        string placeInfo = PlaceId == -1 ? "Global" : $"Place {PlaceId}";
        return $"Capteur n°{Id} | {Type,-12} | {placeInfo,-10} | {etat} | Valeur : {DerniereValeur,6:F1} | Mesure : {date}";
    }

    public override string ToString()
    {
        return $"C{Id}({Type})";
    }
}
