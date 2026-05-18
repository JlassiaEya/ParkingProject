using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ParkingApi.Configuration;
using ParkingApi.Models;
using ParkingConsole.Models;
using ParkingConsole.Services;

namespace ParkingApi.Controllers;

// [ApiController] active des comportements automatiques (validation, erreurs 400, etc.)
[ApiController]
// [Route] définit le préfixe de toutes les routes de ce contrôleur
[Route("api/places")]
public class PlaceController : ControllerBase
{
    // Injection de dépendances : le service est fourni automatiquement par ASP.NET Core
    private readonly IPlaceService _placeService;
    private readonly ILogger<PlaceController> _logger;

    private readonly ParkingSettings _settings;
    //private readonly HealthCheckService _healthCheckService;

 

    public PlaceController(
        IPlaceService placeService,
        ILogger<PlaceController> logger,
        IOptions<ParkingSettings> settings)
    {
        _placeService = placeService;
        _logger = logger;
        _settings = settings.Value;
    }
 
    [HttpGet]
[ProducesResponseType(typeof(ApiResponse<List<Place>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<Place>>> GetAllPlaces()
    {
        _logger.LogInformation("GET /api/place - Début de la récupération de toutes les places");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        List<Place> places = _placeService.Obtenir();
        stopwatch.Stop();

        _logger.LogInformation(
            "GET /api/place - {Count} places récupérées en {ElapsedMs}ms",
            places.Count,
            stopwatch.ElapsedMilliseconds
        );

        return Ok(ApiResponse<List<Place>>.SuccessResponse(places, "Places récupérées avec succès"));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<Place>> GetPlaceById(int id)
    {
        _logger.LogDebug("GET /api/place/{Id} - Recherche de la place", id);

        Place? place = _placeService.Obtenir(id);

        if (place == null)
        {
            _logger.LogWarning("GET /api/place/{Id} - Place non trouvée", id);
            return NotFound(ApiResponse<Place>.ErrorResponse(
                $"La place avec l'ID {id} n'existe pas.",
                new List<string> { "PLACE_NOT_FOUND" }
            ));
        }

        _logger.LogInformation("GET /api/place/{Id} - Place n°{Numero} trouvée", id, place.Numero);
        return Ok(ApiResponse<Place>.SuccessResponse(place));
    }
    /// <summary>
    /// Récupère la configuration du parking
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<ParkingSettings>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<ParkingSettings>> GetConfiguration()
    {
        _logger.LogInformation("GET /api/place/config - Récupération de la configuration");

        return Ok(ApiResponse<ParkingSettings>.SuccessResponse(
            _settings,
            "Configuration récupérée avec succès"
        ));
    }
    /// <summary>
    /// Endpoint de test pour déclencher une exception (DEV ONLY)
    /// </summary>
    [HttpGet("test-error")]
    public ActionResult TestError()
    {
        _logger.LogInformation("GET /api/place/test-error - Déclenchement d'une exception volontaire");
        throw new InvalidOperationException("Ceci est une exception de test !");
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<Place>> UpdatePlace(int id, [FromBody] UpdatePlaceRequest request)
    {
        _logger.LogInformation(
            "PUT /api/place/{Id} - Tentative de mise à jour (estOccupee: {EstOccupee})",
            id,
            request.EstOccupee
        );

        Place? place = _placeService.Obtenir(id);
        if (place == null)
        {
            _logger.LogWarning("PUT /api/place/{Id} - Place non trouvée", id);
            return NotFound(ApiResponse<Place>.ErrorResponse(
                $"La place avec l'ID {id} n'existe pas."
            ));
        }

        // Vérifier si l'état a réellement changé
        if (place.EstOccupee == request.EstOccupee)
        {
            _logger.LogWarning(
                "PUT /api/place/{Id} - Aucun changement d'état (déjà {Etat})",
                id,
                request.EstOccupee ? "occupée" : "libre"
            );
            return BadRequest(ApiResponse<Place>.ErrorResponse(
                $"La place est déjà {(request.EstOccupee ? "occupée" : "libre")}."
            ));
        }

        bool success;
        if (request.EstOccupee)
        {
            success = _placeService.OccuperPlace(id);

            // Vérifier le seuil d'alerte
            var resume = _placeService.ObteniRresume();
            if (resume.TauxOccupation >= _settings.SeuilAlerteOccupation)
            {
                _logger.LogWarning(
                    "⚠️ ALERTE : Taux d'occupation à {Taux}% (seuil: {Seuil}%)",
                    resume.TauxOccupation,
                    _settings.SeuilAlerteOccupation
                );
            }
            _logger.LogInformation("PUT /api/place/{Id} - Place occupée avec succès", id);
        }
        else
        {
            success = _placeService.LibererPlace(id);
            _logger.LogInformation("PUT /api/place/{Id} - Place libérée avec succès", id);
        }

        if (!success)
        {
            _logger.LogError("PUT /api/place/{Id} - Échec de la mise à jour", id);
            return BadRequest(ApiResponse<Place>.ErrorResponse("Impossible de mettre à jour la place."));
        }

        place = _placeService.Obtenir(id);
        _logger.LogInformation("Place mise à jour - PlaceId: {PlaceId}, Numero: {Numero}, Etage: {Etage}, NouvelEtat: {NouvelEtat}",
            place.Id,
            place.Numero,
            place.Etage,
            request.EstOccupee ? "Occupée" : "Libre");
        return Ok(ApiResponse<Place>.SuccessResponse(place!, "Place mise à jour avec succès"));
    }

    [HttpPut("{id}/scope")]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<Place>> UpdatePlaceScope(int id, [FromBody] UpdatePlaceRequest request)
    {
        using (_logger.BeginScope("UpdatePlaceScope - PlaceId: {PlaceId}, UserId: {UserId}", id, "user123"))
        {
            _logger.LogInformation("Début de la mise à jour");

            Place? place = _placeService.Obtenir(id);
            if (place == null)
            {
                _logger.LogWarning("Place non trouvée");
                return NotFound(ApiResponse<Place>.ErrorResponse($"La place avec l'ID {id} n'existe pas."));
            }

            if (place.EstOccupee == request.EstOccupee)
            {
                _logger.LogWarning("Aucun changement d'état (déjà {Etat})", request.EstOccupee ? "occupée" : "libre");
                return BadRequest(ApiResponse<Place>.ErrorResponse(
                    $"La place est déjà {(request.EstOccupee ? "occupée" : "libre")}."
                ));
            }

            bool success;
            if (request.EstOccupee)
            {
                success = _placeService.OccuperPlace(id);
                _logger.LogInformation("Place occupée avec succès");
            }
            else
            {
                success = _placeService.LibererPlace(id);
                _logger.LogInformation("Place libérée avec succès");
            }

            if (!success)
            {
                _logger.LogError("Échec de la mise à jour");
                return BadRequest(ApiResponse<Place>.ErrorResponse("Impossible de mettre à jour la place."));
            }

            place = _placeService.Obtenir(id);

            _logger.LogInformation("Fin de la mise à jour");

            return Ok(ApiResponse<Place>.SuccessResponse(place!, "Place mise à jour avec succès"));
        }
    }
    ///////////////////////////////////////////////

    [HttpGet("paged")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PagedResult<Place>> GetPlacesPerPagination(
 [FromQuery] int page = 1,
 [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation($"GET /api/place?page={page}&pageSize={pageSize}");

        var allPlaces = _placeService.Obtenir();

        int totalItems = allPlaces.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = allPlaces
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

        var result = new PagedResult<Place>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return Ok(result);
    }
    //create place 
    /// <summary>
    /// Crée une nouvelle place dans le parking
    /// </summary>
    /// <param name="request">Les données de la nouvelle place</param>
    /// <returns>La place créée</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Place>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<Place>> CreatePlace([FromBody] CreatePlaceRequest request)
    {
        _logger.LogInformation("POST /api/place - Création d'une nouvelle place n°{Numero}", request.Numero);

        // Vérifier que le numéro n'existe pas déjà
        var places = _placeService.Obtenir();
        if (places.Any(p => p.Numero == request.Numero && p.Etage == request.Etage))
        {
            _logger.LogWarning("Place n°{Numero} à l'étage {Etage} existe déjà", request.Numero, request.Etage);
            return BadRequest(ApiResponse<Place>.ErrorResponse(
                $"Une place n°{request.Numero} existe déjà à l'étage {request.Etage}",
                new List<string> { "Numéro de place en doublon" }
            ));
        }

        // Créer la nouvelle place
        int newId = places.Max(p => p.Id) + 1;
        Place newPlace = new Place(newId, request.Numero.Value, request.Etage, request.Description);

        // Ajouter via le repository
        var repository = _placeService.Obtenir(); // On récupère le repository indirectement
                                                  // Note : idéalement, il faudrait une méthode Ajouter dans IPlaceService
                                                  // Pour ce TP, on va simuler l'ajout

        _logger.LogInformation("Place n°{Numero} créée avec succès (ID: {Id})", newPlace.Numero, newPlace.Id);

        return CreatedAtAction(
            nameof(GetPlaceById),
            new { id = newPlace.Id },
            ApiResponse<Place>.SuccessResponse(newPlace, "Place créée avec succès")
        );
    }

    /// <summary>
    /// Récupère uniquement les places libres
    /// </summary>
    /// <returns>Liste des places disponibles</returns>
    /// 
    [HttpGet("libres")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<Place>> GetPlacesLibres()
    {
        _logger.LogInformation("GET /api/place/libres - Récupération des places libres");

        List<Place> placesLibres = _placeService.ObteniePlacesLibres();

        return Ok(placesLibres);
    }
    /// <summary>
    /// Récupère un résumé de l'état du parking
    /// </summary>
    /// <returns>Statistiques du parking</returns>
    [HttpGet("resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ResumeParkingDto> GetResume()
    {
        _logger.LogInformation("GET /api/place/resume - Récupération du résumé");

        ResumeParkingDto resume = _placeService.ObteniRresume();

        return Ok(resume);
    }
    [HttpGet("etage/{etage}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<Place>> GetPlacesByEtage(int etage)
    {
        _logger.LogInformation($"GET /api/place/etage/{etage}");

        var places = _placeService.Obtenir()
                                  .Where(p => p.Etage == etage)
                                  .ToList();

        return Ok(places);
    }
   
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeletePlace(int id)
    {
        _logger.LogInformation($"DELETE /api/place/{id}");

        var place = _placeService.Obtenir(id);

        if (place == null)
            return NotFound(new { message = $"Place {id} introuvable" });

        _placeService.Supprimer(id);

        return NoContent();
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetStats()
    {
        _logger.LogInformation("GET /api/place/stats - Calcul des statistiques");

        var places = _placeService.Obtenir();

        int total = places.Count;

        var parEtage = places
            .GroupBy(p => p.Etage)
            .Select(g => new
            {
                Etage = g.Key,
                Total = g.Count(),
                Occupees = g.Count(p => p.EstOccupee),
                Libres = g.Count(p => !p.EstOccupee),
                TauxOccupation = g.Count(p => p.EstOccupee) * 100.0 / g.Count()
            });

        var stats = new
        {
            NombreTotalPlaces = total,
            StatistiquesParEtage = parEtage,
            TempsMoyenOccupationMinutes = 120
        };

        return Ok(ApiResponse<object>.SuccessResponse(stats, "Statistiques calculées"));
    }
}
