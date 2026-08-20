using OmniCore.InventoryService.Models.DTOs;

namespace OmniCore.InventoryService.Services.Interfaces;

public interface IInventoryService
{
    Task<InventoryResponse?> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<InventoryResponse> UpdateAsync(
        Guid productId,
        UpdateInventoryRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryResponse?> ReserveAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<InventoryResponse?> ReleaseAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);
}