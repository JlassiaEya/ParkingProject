using Microsoft.AspNetCore.Mvc;
using ServiceHistorique.EventStore;
using ServiceHistorique.Projections;

namespace ServiceHistorique.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvenementController : ControllerBase
{
    private readonly IEventStore _store;
    private readonly IPlaceProjection _projection;

    public EvenementController(IEventStore store, IPlaceProjection projection)
    {
        _store = store;
        _projection = projection;
    }

    // ── Event Store — lecture brute ───────────────────────────────────

    /// <summary>Retourne tous les événements stockés (append-only log).</summary>
    [HttpGet("all")]
    public IActionResult GetAll()
        => Ok(new { count = _store.Count, events = _store.GetAll() });

    /// <summary>Retourne les événements d'une place spécifique.</summary>
    [HttpGet("place/{placeId:int}")]
    public IActionResult GetByPlace(int placeId)
    {
        var evts = _store.GetByPlaceId(placeId);
        return Ok(new { placeId, count = evts.Count, events = evts });
    }

    /// <summary>Retourne les événements d'un type donné.</summary>
    [HttpGet("type/{type}")]
    public IActionResult GetByType(string type)
    {
        var evts = _store.GetByType(type);
        return Ok(new { type, count = evts.Count, events = evts });
    }

    // ── Projections — état reconstruit par rejeu ──────────────────────

    /// <summary>
    /// Rejoue tous les événements et retourne l'état actuel de chaque place.
    /// </summary>
    [HttpGet("projection/places")]
    public IActionResult GetProjection()
    {
        var etats = _projection.ProjecterTout();
        return Ok(new
        {
            totalEvenements = _store.Count,
            places = etats
        });
    }

    /// <summary>
    /// Rejoue les événements d'une place jusqu'à un instant T.
    /// Permet de voir l'état qu'avait la place à n'importe quel moment passé.
    /// </summary>
    [HttpGet("projection/place/{placeId:int}")]
    public IActionResult GetProjectionPlace(
        int placeId,
        [FromQuery] DateTime? jusqu_au = null)
    {
        var etat = _projection.ProjecterPlace(placeId, jusqu_au);
        if (etat is null) return NotFound();

        return Ok(new
        {
            placeId,
            jusqu_au = jusqu_au?.ToString("o") ?? "maintenant",
            etatReconstruit = etat
        });
    }

    // ── Statistiques ──────────────────────────────────────────────────

    /// <summary>Statistiques globales de l'Event Store.</summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var all = _store.GetAll();
        var places = _projection.ProjecterTout();
        return Ok(new
        {
            totalEvenements = _store.Count,
            placesOccupees = places.Count(p => p.EstOccupee),
            placesLibres = places.Count(p => !p.EstOccupee),
            tauxOccupation = places.Any()
                                    ? $"{places.Count(p => p.EstOccupee) * 100 / places.Count}%"
                                    : "N/A",
            typesEvenements = all.GroupBy(e => e.Type)
                                    .Select(g => new { type = g.Key, count = g.Count() })
        });
    }
    /// <summary>Vide les snapshots et rejoue tous les événements depuis le début</summary>
    [HttpPost("replay")]
    public ActionResult<List<EtatPlace>> Replay()
    {
        // Vide les snapshots
        _store.Reset();

        // Rejoue pour toutes les places connues
        var placeIds = _store.GetAll()
                                  .Select(e => e.PlaceId)
                                  .Distinct();

        var etats = placeIds.Select(id => _projection.ProjecterPlace(id)).ToList();

        return Ok(new
        {
            message = "Replay complet effectué",
            nombreEvenements = _store.GetAll().Count,
            etats
        });
    }
}
