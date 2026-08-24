using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;
using OmniCore.ProductService.Health;

namespace OmniCore.ProductService.Health;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(
        IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _redis.GetDatabase();

            var latency = await database.PingAsync();

            return HealthCheckResult.Healthy(
                $"Redis responded in {latency.TotalMilliseconds:F0} ms.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Redis is unavailable.",
                exception);
        }
    }
}