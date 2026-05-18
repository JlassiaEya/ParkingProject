using Microsoft.AspNetCore.Mvc;
using ServiceHistorique.Services;

namespace ServiceHistorique.Controllers;

[ApiController]
[Route("api/historique")]
public class HistoriqueController : ControllerBase
{
    private readonly HistoriqueRepository _repo;
    public HistoriqueController(HistoriqueRepository repo) => _repo = repo;

    // GET /api/historique?placeId=3
    [HttpGet]
    public IActionResult Get([FromQuery] int? placeId)
    {
        var data = placeId.HasValue ? _repo.GetParPlace(placeId.Value) : _repo.GetTous();
        return Ok(data);
    }

    // GET /api/historique/3/etat?at=2026-05-15T08:00:00Z
    [HttpGet("{placeId:int}/etat")]
    public IActionResult GetEtatAt(int placeId, [FromQuery] DateTime at)
    {
        var etat = _repo.ReconstruireEtat(placeId, at);
        if (etat is null) return NotFound(new { message = "Aucun événement trouvé" });
        return Ok(new { placeId, at, estOccupee = etat });
    }

    // GET /api/historique/3/stats
    [HttpGet("{placeId:int}/stats")]
    public IActionResult GetStats(int placeId) => Ok(_repo.CalculerStats(placeId));
}