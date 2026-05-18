using ServiceHistorique.Models;
using System.Collections.Concurrent;

namespace ServiceHistorique.Services;

public class HistoriqueRepository
{
	private readonly ConcurrentBag<EvenementHistorique> _evenements = new();

	// Ajouter un événement
	public void Ajouter(EvenementHistorique evt) => _evenements.Add(evt);

	// Tous les événements d'une place, triés par date
	public IEnumerable<EvenementHistorique> GetParPlace(int placeId) =>
		_evenements
			.Where(e => e.PlaceId == placeId)
			.OrderBy(e => e.Timestamp);

	// Tous les événements
	public IEnumerable<EvenementHistorique> GetTous() =>
		_evenements.OrderBy(e => e.Timestamp);

	// Reconstruire l'état d'une place à un instant T
	public bool? ReconstruireEtat(int placeId, DateTime instantT) =>
		_evenements
			.Where(e => e.PlaceId == placeId && e.Timestamp <= instantT)
			.OrderByDescending(e => e.Timestamp)
			.FirstOrDefault()?.EstOccupee;

	// Statistiques
	public object CalculerStats(int placeId)
	{
		var evts = GetParPlace(placeId).ToList();
		if (evts.Count < 2)
			return new { message = "Pas assez de données" };

		// Durée moyenne d'occupation
		var durees = new List<double>();
		for (int i = 0; i < evts.Count - 1; i++)
			if (evts[i].EstOccupee)
				durees.Add((evts[i + 1].Timestamp - evts[i].Timestamp).TotalMinutes);

		// Heures de pointe
		var heuresDePointe = evts
			.Where(e => e.EstOccupee)
			.GroupBy(e => e.Timestamp.Hour)
			.OrderByDescending(g => g.Count())
			.Take(3)
			.Select(g => new { heure = $"{g.Key}h00", occurrences = g.Count() });

		return new
		{
			placeId,
			totalEvenements = evts.Count,
			duréeMoyenneOccupMin = durees.Any() ? Math.Round(durees.Average(), 1) : 0,
			heuresDePointe
		};
	}
}