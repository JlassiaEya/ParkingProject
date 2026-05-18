namespace ServiceQualite.Models;

public class DonneesQualite
{
    public int Co2Ppm { get; set; }
    public double Temperature { get; set; }
    public DateTime Timestamp { get; set; }
    public string Niveau => Co2Ppm switch
    {
        < 600  => "Excellent",
        < 1000 => "Bon",
        < 1500 => "Moyen",
        _      => "Mauvais"
    };
}
