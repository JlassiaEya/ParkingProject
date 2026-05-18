using ApiGateway.Services;

namespace ApiGateway.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly int _maxRequests;

    // Cle = token client, Valeur = (nombre de requetes, debut de la fenetre)
    private readonly Dictionary<string, (int Count, DateTime WindowStart)> _counters = new();
    private readonly object _lock = new();

    private readonly GatewayStats _stats;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration config,
        GatewayStats stats)
    {
        _next = next;
        _logger = logger;
        _maxRequests = config.GetValue<int>("Gateway:RateLimit:MaxRequestsPerMinute", 20);
        _stats = stats;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Identifier le client (token ou IP si pas de token)
        var clientId = context.Request.Headers.TryGetValue("X-Api-Key", out var key)
            ? key.ToString()
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        lock (_lock)
        {
            var now = DateTime.UtcNow;

            if (_counters.TryGetValue(clientId, out var entry))
            {
                // Reinitialiser la fenetre si une minute est ecoulee
                if ((now - entry.WindowStart).TotalSeconds >= 60)
                {
                    _counters[clientId] = (1, now);
                }
                else if (entry.Count >= _maxRequests)
                {
                    _logger.LogWarning(
                        "[RateLimit] Client {ClientId} bloque : {Count}/{Max} req/min",
                        clientId, entry.Count, _maxRequests);

                    _stats.IncrementRateLimitBlocked();

                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers["Retry-After"] = "60";

                    // Ne pas attendre le Task (fire-and-forget acceptable ici)
                    context.Response.WriteAsJsonAsync(new
                    {
                        Error = "Trop de requetes",
                        RetryAfterSeconds = 60,
                        LimiteParMinute = _maxRequests
                    }).GetAwaiter().GetResult();
                    return;
                }
                else
                {
                    _counters[clientId] = (entry.Count + 1, entry.WindowStart);
                }
            }
            else
            {
                _counters[clientId] = (1, now);
            }
        }

        await _next(context);
    }
}
