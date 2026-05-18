using System.Threading;

namespace ApiGateway.Services;

public class GatewayStats
{
    private int _totalRequests;
    private int _authBlocked;
    private int _rateLimitBlocked;
    private int _forwarded;

    public int TotalRequests => _totalRequests;
    public int AuthBlocked => _authBlocked;
    public int RateLimitBlocked => _rateLimitBlocked;
    public int Forwarded => _forwarded;

    public void IncrementTotalRequests() => Interlocked.Increment(ref _totalRequests);
    public void IncrementAuthBlocked() => Interlocked.Increment(ref _authBlocked);
    public void IncrementRateLimitBlocked() => Interlocked.Increment(ref _rateLimitBlocked);
    public void IncrementForwarded() => Interlocked.Increment(ref _forwarded);
}
