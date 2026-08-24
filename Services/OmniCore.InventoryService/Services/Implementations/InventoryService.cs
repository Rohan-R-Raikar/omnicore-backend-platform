using Microsoft.EntityFrameworkCore;
using OmniCore.InventoryService.Models.DTOs;
using OmniCore.InventoryService.Models.Entities;
using OmniCore.InventoryService.Models.Exceptions;
using OmniCore.InventoryService.Repositories.Interfaces;
using OmniCore.InventoryService.Services.Interfaces;

namespace OmniCore.InventoryService.Services.Implementations;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        ILogger<InventoryService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<InventoryResponse?> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var inventory =
            await _inventoryRepository.GetByProductIdAsync(
                productId,
                cancellationToken);

        return inventory is null
            ? null
            : MapInventory(inventory);
    }

    public async Task<InventoryResponse> UpdateAsync(
        Guid productId,
        UpdateInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var inventory =
            await _inventoryRepository.GetTrackedByProductIdAsync(
                productId,
                cancellationToken);

        if (inventory is null)
        {
            inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                AvailableQuantity = request.AvailableQuantity,
                ReservedQuantity = request.ReservedQuantity,
                UpdatedAt = DateTime.UtcNow
            };

            await _inventoryRepository.AddAsync(
                inventory,
                cancellationToken);
        }
        else
        {
            inventory.AvailableQuantity =
                request.AvailableQuantity;

            inventory.ReservedQuantity =
                request.ReservedQuantity;

            inventory.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            await _inventoryRepository.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConcurrencyException();
        }

        _logger.LogInformation(
            "Inventory updated for ProductId {ProductId}. " +
            "Available: {AvailableQuantity}, Reserved: {ReservedQuantity}",
            productId,
            inventory.AvailableQuantity,
            inventory.ReservedQuantity);

        return MapInventory(inventory);
    }

    public async Task<InventoryResponse?> ReserveAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Reservation quantity must be greater than zero.");
        }

        var inventory =
            await _inventoryRepository.GetTrackedByProductIdAsync(
                productId,
                cancellationToken);

        if (inventory is null)
        {
            return null;
        }

        if (inventory.AvailableQuantity < quantity)
        {
            throw new InvalidOperationException(
                "Insufficient inventory.");
        }

        inventory.AvailableQuantity -= quantity;
        inventory.ReservedQuantity += quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _inventoryRepository.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConcurrencyException();
        }

        _logger.LogInformation(
            "Reserved {Quantity} units for ProductId {ProductId}. " +
            "Available: {AvailableQuantity}, Reserved: {ReservedQuantity}",
            quantity,
            productId,
            inventory.AvailableQuantity,
            inventory.ReservedQuantity);

        return MapInventory(inventory);
    }

    public async Task<InventoryResponse?> ReleaseAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Release quantity must be greater than zero.");
        }

        var inventory =
            await _inventoryRepository.GetTrackedByProductIdAsync(
                productId,
                cancellationToken);

        if (inventory is null)
        {
            return null;
        }

        if (inventory.ReservedQuantity < quantity)
        {
            throw new InvalidOperationException(
                "Cannot release more inventory than is currently reserved.");
        }

        inventory.ReservedQuantity -= quantity;
        inventory.AvailableQuantity += quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _inventoryRepository.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConcurrencyException();
        }

        _logger.LogInformation(
            "Released {Quantity} reserved units for ProductId {ProductId}. " +
            "Available: {AvailableQuantity}, Reserved: {ReservedQuantity}",
            quantity,
            productId,
            inventory.AvailableQuantity,
            inventory.ReservedQuantity);

        return MapInventory(inventory);
    }

    private static InventoryResponse MapInventory(
        Inventory inventory)
    {
        return new InventoryResponse
        {
            Id = inventory.Id,
            ProductId = inventory.ProductId,
            AvailableQuantity = inventory.AvailableQuantity,
            ReservedQuantity = inventory.ReservedQuantity,
            UpdatedAt = inventory.UpdatedAt
        };
    }
}