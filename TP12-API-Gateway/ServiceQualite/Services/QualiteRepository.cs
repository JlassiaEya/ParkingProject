using ServiceQualite.Models;

namespace ServiceQualite.Services;

public class QualiteRepository : IQualiteRepository
{
    private readonly List<DonneesQualite> _mesures = new();
    private readonly object _lock = new();

    public void AjouterMesure(DonneesQualite mesure)
    {
        lock (_lock)
        {
            _mesures.Add(mesure);
            // On garde seulement les 100 dernières mesures
            if (_mesures.Count > 100)
            {
                _mesures.RemoveAt(0);
            }
        }
    }

    public DonneesQualite? GetDerniereMesure()
    {
        lock (_lock)
        {
            return _mesures.LastOrDefault();
        }
    }

    public IEnumerable<DonneesQualite> GetHistorique()
    {
        lock (_lock)
        {
            return _mesures.ToList();
        }
    }
}
