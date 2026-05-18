namespace ApiGateway.Services;

public class ProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProxyService> _logger;
    private readonly Dictionary<string, string> _routes;
    private readonly int _timeoutSeconds;

    public ProxyService(
        HttpClient httpClient,
        ILogger<ProxyService> logger,
        IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _routes = config.GetSection("Gateway:Routes")
                        .Get<Dictionary<string, string>>() ?? new();
        _timeoutSeconds = config.GetValue<int>("Gateway:TimeoutSeconds", 5);
    }

    /// <summary>
    /// Resout l'URL cible a partir du chemin de la requete entrante.
    /// Ex: /api/places/libres -> http://localhost:5001/api/places/libres
    /// </summary>
    public string? ResolveTarget(string path)
    {
        foreach (var (prefix, baseUrl) in _routes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return baseUrl + path;
        }
        return null;
    }

    /// <summary>
    /// Transfere la requete vers l'URL cible et retourne la reponse.
    /// </summary>
    public async Task<HttpResponseMessage> ForwardAsync(
        HttpRequest incomingRequest,
        string targetUrl,
        CancellationToken ct = default)
    {
        // Construire la requete vers le microservice
        var outgoing = new HttpRequestMessage
        {
            Method = new HttpMethod(incomingRequest.Method),
            RequestUri = new Uri(targetUrl)
        };

        // Copier les headers pertinents (sauf Host qui serait incorrect)
        foreach (var header in incomingRequest.Headers)
        {
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
            if (header.Key.Equals("X-Api-Key", StringComparison.OrdinalIgnoreCase)) continue;
            outgoing.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        // Copier le corps pour POST/PUT/PATCH
        if (incomingRequest.ContentLength > 0 || incomingRequest.Headers.ContainsKey("Transfer-Encoding"))
        {
            outgoing.Content = new StreamContent(incomingRequest.Body);
            if (incomingRequest.ContentType is not null)
                outgoing.Content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(incomingRequest.ContentType);
        }

        _logger.LogInformation(
            "[Proxy] {Method} {Source} -> {Target}",
            incomingRequest.Method, incomingRequest.Path, targetUrl);

        // Ex3: Ajouter les headers de contexte
        var clientIp = incomingRequest.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        outgoing.Headers.TryAddWithoutValidation("X-Forwarded-For", clientIp);
        outgoing.Headers.TryAddWithoutValidation("X-Gateway-Request-Id", Guid.NewGuid().ToString());

        // Ex2: Timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

       return await _httpClient.SendAsync(outgoing, HttpCompletionOption.ResponseContentRead, cts.Token);

    }
}
