using OmniCore.OrderService.Models.External;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Services.Clients;

public interface IProductServiceClient
{
    Task<ProductDto?> GetProductAsync(
        Guid productId,
        string authorizationHeader,
        CancellationToken cancellationToken = default);
}