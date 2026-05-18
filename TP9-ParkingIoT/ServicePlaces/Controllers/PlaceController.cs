using Microsoft.AspNetCore.Mvc;
using ServicePlaces.Services;

namespace ServicePlaces.Controllers;

[ApiController]
[Route("api/places")]
public class PlaceController : ControllerBase
{
    private readonly IPlaceRepository _repository;
    private readonly ILogger<PlaceController> _logger;

    public PlaceController(
        IPlaceRepository repository,
        ILogger<PlaceController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Retourne la liste complète des places avec leur état actuel.</summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        _logger.LogInformation("[REST] GET /api/places");
        return Ok(_repository.GetAll());
    }

    /// <summary>Retourne une place spécifique par son identifiant.</summary>
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var place = _repository.GetById(id);
        if (place is null)
        {
            _logger.LogWarning("[REST] Place {Id} non trouvée", id);
            return NotFound(new { message = $"Place {id} introuvable" });
        }
        return Ok(place);
    }

    /// <summary>Retourne uniquement les places libres.</summary>
    [HttpGet("libres")]
    public IActionResult GetLibres()
    {
        var libres = _repository.GetLibres();
        _logger.LogInformation("[REST] {Count} places libres", libres.Count());
        return Ok(libres);
    }

    /// <summary>Endpoint de santé du microservice.</summary>
    [HttpGet("/api/health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "UP",
            service = "Service Places",
            timestamp = DateTime.UtcNow
        });
    }
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var stats = _repository.GetStats();

        _logger.LogInformation(
            "[REST] Stats demandées | Occupation : {Taux}%",
            stats.tauxOccupation);

        return Ok(new
        {
            totalPlaces = stats.total,
            placesOccupees = stats.occupees,
            placesLibres = stats.libres,
            tauxOccupation = $"{stats.tauxOccupation}%",
            derniereMiseAJour = stats.derniereMiseAJour
        });
    }

   

    [HttpGet("{id:int}/historique")]
    public IActionResult GetHistorique(int id)
    {
        var place = _repository.GetById(id);
        if (place is null)
        {
            return NotFound(new { message = $"Place {id} introuvable" });
        }

        var historique = _repository.GetHistorique(id);

        _logger.LogInformation(
            "[REST] Historique place {Id} : {Count} événements",
            id, historique.Count());

        return Ok(new
        {
            placeId = id,
            numero = place.Numero,
            nombreEvenements = historique.Count(),
            evenements = historique
        });
    }
}
