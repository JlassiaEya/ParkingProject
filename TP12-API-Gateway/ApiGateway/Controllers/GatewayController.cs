using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class GatewayController : ControllerBase
{
    private readonly ProxyService _proxy;
    private readonly ILogger<GatewayController> _logger;
    private readonly GatewayStats _stats;

    public GatewayController(ProxyService proxy, ILogger<GatewayController> logger, GatewayStats stats)
    {
        _proxy = proxy;
        _logger = logger;
        _stats = stats;
    }

    [Route("api/{**path}")]
    public async Task<IActionResult> ProxyAll(string path)
    {
        var fullPath = "/api/" + path;

        if (Request.QueryString.HasValue)
            fullPath += Request.QueryString.Value;

        var targetUrl = _proxy.ResolveTarget(fullPath);

        if (targetUrl is null)
        {
            _logger.LogWarning("[Gateway] Route inconnue : {Path}", fullPath);
            return NotFound(new { Error = $"Aucun service configure pour : {fullPath}" });
        }

        try
        {
            var response = await _proxy.ForwardAsync(Request, targetUrl);

            _stats.IncrementForwarded();

            // ✅ Copier les headers SAUF ceux liés au transfert
            var headersAExclure = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Transfer-Encoding",
                "Content-Encoding",
                "Content-Length"
            };

            foreach (var header in response.Headers)
            {
                if (!headersAExclure.Contains(header.Key))
                    Response.Headers[header.Key] = header.Value.ToArray();
            }

            var body = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[Gateway] Timeout pour le microservice {Target}", targetUrl);
            return StatusCode(504, new { Error = "Gateway Timeout", Detail = "Le microservice a mis trop de temps a repondre" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[Gateway] Microservice inaccessible : {Target}", targetUrl);
            return StatusCode(502, new { Error = "Microservice inaccessible", Detail = ex.Message });
        }
    }

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { Status = "Gateway OK", Heure = DateTime.UtcNow });

    [HttpGet("/gateway/stats")]
    public IActionResult GetStats()
    {
        return Ok(new
        {
            TotalRequests = _stats.TotalRequests,
            AuthBlocked = _stats.AuthBlocked,
            RateLimitBlocked = _stats.RateLimitBlocked,
            Forwarded = _stats.Forwarded
        });
    }
}