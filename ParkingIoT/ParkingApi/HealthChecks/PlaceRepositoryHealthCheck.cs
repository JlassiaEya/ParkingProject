using Microsoft.Extensions.Diagnostics.HealthChecks;
using ParkingConsole.Repositories;

namespace ParkingApi.HealthChecks;

/// <summary>
/// Health check personnalisé pour vérifier l'état du repository des places
/// </summary>
public class PlaceRepositoryHealthCheck : IHealthCheck
{
    private readonly IPlaceRepository _placeRepository;
    private readonly ILogger<PlaceRepositoryHealthCheck> _logger;

    public PlaceRepositoryHealthCheck(
        IPlaceRepository placeRepository,
        ILogger<PlaceRepositoryHealthCheck> logger)
    {
        _placeRepository = placeRepository;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Tenter de récupérer les places
            var places = _placeRepository.Obtenir();
            int count = places.Count;

            if (count == 0)
            {
                _logger.LogWarning("Health check : Aucune place trouvée dans le repository");
                return Task.FromResult(
                    HealthCheckResult.Degraded("Le repository est vide")
                );
            }

            _logger.LogDebug("Health check : {Count} places trouvées", count);
            return Task.FromResult(
                HealthCheckResult.Healthy($"{count} places disponibles")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check : Erreur lors de l'accès au repository");
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Impossible d'accéder au repository", ex)
            );
        }
    }
}
