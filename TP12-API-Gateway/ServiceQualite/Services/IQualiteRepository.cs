using ServiceQualite.Models;

namespace ServiceQualite.Services;

public interface IQualiteRepository
{
    DonneesQualite? GetDerniereMesure();
    IEnumerable<DonneesQualite> GetHistorique();
    void AjouterMesure(DonneesQualite mesure);
}
