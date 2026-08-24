using OmniCore.OrderService.Models.External;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Services.Clients;

public interface IInventoryServiceClient
{
    Task<InventoryDto?> ReserveAsync(
        Guid productId,
        int quantity,
        string authorizationHeader,
        CancellationToken cancellationToken = default);

    Task<InventoryDto?> ReleaseAsync(
        Guid productId,
        int quantity,
        string authorizationHeader,
        CancellationToken cancellationToken = default);
}