using Microsoft.Extensions.Diagnostics.HealthChecks;
using ParkingConsole.Services;

namespace ParkingApi.HealthChecks;

public class OccupationRateHealthCheck : IHealthCheck
{
    private readonly IPlaceService _placeService;
    private readonly ILogger<OccupationRateHealthCheck> _logger;

    public OccupationRateHealthCheck(
        IPlaceService placeService,
        ILogger<OccupationRateHealthCheck> logger)
    {
        _placeService = placeService;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var resume = _placeService.ObteniRresume();
        double taux = resume.TauxOccupation;

        _logger.LogInformation("HealthCheck Occupation: {Taux}%", taux);

        if (taux < 80)
            return Task.FromResult(
                HealthCheckResult.Healthy($"Taux normal : {taux}%")
            );

        if (taux < 95)
            return Task.FromResult(
                HealthCheckResult.Degraded($"Taux élevé : {taux}%")
            );

        return Task.FromResult(
            HealthCheckResult.Unhealthy($"Parking presque plein : {taux}%")
        );
    }
}