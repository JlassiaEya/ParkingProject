namespace ApiGateway.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly HashSet<string> _validKeys;

    // Routes publiques qui ne necessitent pas de token
    private static readonly string[] PublicRoutes = { "/swagger", "/health" };

    public AuthenticationMiddleware(
        RequestDelegate next,
        ILogger<AuthenticationMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        var keys = config.GetSection("Gateway:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
        _validKeys = new HashSet<string>(keys, StringComparer.Ordinal);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Laisser passer les routes publiques
        if (PublicRoutes.Any(r => path.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Extraire le token du header
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var token))
        {
            _logger.LogWarning("[Auth] Requete rejetee : header X-Api-Key absent. Path={Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Error = "Header X-Api-Key requis" });
            return;
        }

        // Verifier la validite
        if (!_validKeys.Contains(token.ToString()))
        {
            _logger.LogWarning("[Auth] Requete rejetee : token invalide. Path={Path}", path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Error = "Token non autorise" });
            return;
        }

        _logger.LogDebug("[Auth] Requete autorisee. Path={Path}", path);
        await _next(context);
    }
}
