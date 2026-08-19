using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OmniCore.ProductService.Caching;

namespace OmniCore.ProductService.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IConfiguration configuration)
    {
        _redis = redis;
        _configuration = configuration;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        var database = _redis.GetDatabase();

        var value = await database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var database = _redis.GetDatabase();

        var json = JsonSerializer.Serialize(value);

        var defaultTtl =
            _configuration.GetValue<int>("Redis:DefaultTtlMinutes");

        var ttl = expiry ?? TimeSpan.FromMinutes(defaultTtl);

        await database.StringSetAsync(
            key,
            json,
            ttl);
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var database = _redis.GetDatabase();

        await database.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        var endpoints = _redis.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);

            await foreach (var key in server.KeysAsync(
                               pattern: $"{prefix}*"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _redis
                    .GetDatabase()
                    .KeyDeleteAsync(key);
            }
        }
    }
}