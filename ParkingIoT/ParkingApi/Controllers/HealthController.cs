using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ParkingApi.Models;

namespace ParkingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Vérifie l'état de santé détaillé de l'API
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<object>>> GetHealth()
    {
        _logger.LogInformation("GET /api/health - Vérification de l'état de santé");

        var healthReport = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = healthReport.Status.ToString(),
            duration = healthReport.TotalDuration.TotalMilliseconds,
            checks = healthReport.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            }),
            timestamp = DateTime.UtcNow
        };

        if (healthReport.Status == HealthStatus.Healthy)
        {
            return Ok(ApiResponse<object>.SuccessResponse(response, "API en bonne santé"));
        }
        else
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.ErrorResponse("API dégradée ou non disponible")
            );
        }
    }

    /// <summary>
    /// Endpoint simple pour vérifier que l'API répond (liveness)
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> Ping()
    {
        return Ok(ApiResponse<object>.SuccessResponse(
            new { message = "pong", timestamp = DateTime.UtcNow },
            "API opérationnelle"
        ));
    }
    /// <summary>
    /// Health check uniquement pour le taux d’occupation
    /// </summary>
    [HttpGet("occupation")]
    public async Task<ActionResult<ApiResponse<object>>> GetOccupationHealth()
    {
        _logger.LogInformation("GET /api/health/occupation");

        var report = await _healthCheckService.CheckHealthAsync(
            check => check.Name == "occupation_rate"
        );

        var result = report.Entries.First().Value;

        var response = new
        {
            status = result.Status.ToString(),
            description = result.Description,
            timestamp = DateTime.UtcNow
        };

        if (result.Status == HealthStatus.Healthy)
            return Ok(ApiResponse<object>.SuccessResponse(response));

        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            ApiResponse<object>.ErrorResponse("Taux d’occupation critique")
        );
    }
}