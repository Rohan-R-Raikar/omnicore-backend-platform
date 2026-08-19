using System;
using System.Threading;
using System.Threading.Tasks;
using OmniCore.ProductService.Caching;

namespace OmniCore.ProductService.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default);
}