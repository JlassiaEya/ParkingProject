using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ApiGateway.Services;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly TokenValidationParameters _validationParams;

    private static readonly string[] PublicRoutes = { "/swagger", "/health", "/auth" };

    private readonly GatewayStats _stats;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger,
        IConfiguration config,
        GatewayStats stats)
    {
        _next   = next;
        _logger = logger;
        _stats  = stats;

        var secretKey = config["Jwt:SecretKey"]!;
        var issuer    = config["Jwt:Issuer"]!;
        var audience  = config["Jwt:Audience"]!;

        _validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer   = true,
            ValidIssuer      = issuer,
            ValidateAudience = true,
            ValidAudience    = audience,
            ValidateLifetime = true,      // Verifier la date d'expiration
            ClockSkew        = TimeSpan.Zero  // Pas de tolerance sur l'expiration
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (PublicRoutes.Any(r => path.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Extraire le token du header Authorization: Bearer <token>
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _stats.IncrementAuthBlocked();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Header Authorization requis : Bearer <token>"
            });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();


        try
        {
            var handler    = new JwtSecurityTokenHandler();
            var principal  = handler.ValidateToken(token, _validationParams, out var validatedToken);
            context.User   = principal;
            _logger.LogDebug("[JWT] Token valide pour : {User}", principal.Identity?.Name);
            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            _stats.IncrementAuthBlocked();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Error = "Token expire, veuillez vous reconnecter" });
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("[JWT] Token invalide : {Msg}", ex.Message);
            _stats.IncrementAuthBlocked();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Error = "Token invalide" });
        }
    }
}
