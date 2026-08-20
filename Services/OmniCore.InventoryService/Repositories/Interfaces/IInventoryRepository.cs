using OmniCore.InventoryService.Models.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.InventoryService.Repositories.Interfaces;

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Inventory?> GetTrackedByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Inventory inventory,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}