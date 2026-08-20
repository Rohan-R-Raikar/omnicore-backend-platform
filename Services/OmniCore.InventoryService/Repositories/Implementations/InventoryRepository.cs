using Microsoft.EntityFrameworkCore;
using OmniCore.InventoryService.Data;
using OmniCore.InventoryService.Models.Entities;
using OmniCore.InventoryService.Repositories.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.InventoryService.Repositories.Implementations;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;

    public InventoryRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Inventory?> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                inventory => inventory.ProductId == productId,
                cancellationToken);
    }

    public async Task<Inventory?> GetTrackedByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Inventories
            .FirstOrDefaultAsync(
                inventory => inventory.ProductId == productId,
                cancellationToken);
    }

    public async Task AddAsync(
        Inventory inventory,
        CancellationToken cancellationToken = default)
    {
        await _context.Inventories.AddAsync(
            inventory,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}