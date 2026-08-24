using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OmniCore.InventoryService.Models.DTOs;
using OmniCore.InventoryService.Models.Entities;
using OmniCore.InventoryService.Models.Exceptions;
using OmniCore.InventoryService.Repositories.Interfaces;

using InventoryServiceImpl =
    OmniCore.InventoryService.Services.Implementations.InventoryService;

namespace OmniCore.InventoryService.Tests;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepository;
    private readonly Mock<ILogger<InventoryServiceImpl>> _logger;

    private readonly InventoryServiceImpl _inventoryService;

    public InventoryServiceTests()
    {
        _inventoryRepository = new Mock<IInventoryRepository>();
        _logger = new Mock<ILogger<InventoryServiceImpl>>();

        _inventoryService = new InventoryServiceImpl(
            _inventoryRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetByProductIdAsync_WhenInventoryExists_ReturnsInventory()
    {
        var productId = Guid.NewGuid();

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            AvailableQuantity = 50,
            ReservedQuantity = 5,
            UpdatedAt = DateTime.UtcNow
        };

        _inventoryRepository
            .Setup(repository => repository.GetByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var result =
            await _inventoryService.GetByProductIdAsync(productId);

        Assert.NotNull(result);
        Assert.Equal(productId, result.ProductId);
        Assert.Equal(50, result.AvailableQuantity);
        Assert.Equal(5, result.ReservedQuantity);
    }

    [Fact]
    public async Task GetByProductIdAsync_WhenInventoryDoesNotExist_ReturnsNull()
    {
        var productId = Guid.NewGuid();

        _inventoryRepository
            .Setup(repository => repository.GetByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        var result =
            await _inventoryService.GetByProductIdAsync(productId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenInventoryDoesNotExist_CreatesInventory()
    {
        var productId = Guid.NewGuid();

        var request = new UpdateInventoryRequest
        {
            AvailableQuantity = 100,
            ReservedQuantity = 0
        };

        _inventoryRepository
            .Setup(repository => repository.GetTrackedByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Inventory?)null);

        var result = await _inventoryService.UpdateAsync(
            productId,
            request);

        Assert.Equal(productId, result.ProductId);
        Assert.Equal(100, result.AvailableQuantity);
        Assert.Equal(0, result.ReservedQuantity);

        _inventoryRepository.Verify(
            repository => repository.AddAsync(
                It.Is<Inventory>(inventory =>
                    inventory.ProductId == productId &&
                    inventory.AvailableQuantity == 100 &&
                    inventory.ReservedQuantity == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _inventoryRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenInventoryExists_UpdatesInventory()
    {
        var productId = Guid.NewGuid();

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            AvailableQuantity = 20,
            ReservedQuantity = 5,
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var request = new UpdateInventoryRequest
        {
            AvailableQuantity = 75,
            ReservedQuantity = 10
        };

        _inventoryRepository
            .Setup(repository => repository.GetTrackedByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var result = await _inventoryService.UpdateAsync(
            productId,
            request);

        Assert.Equal(75, result.AvailableQuantity);
        Assert.Equal(10, result.ReservedQuantity);

        _inventoryRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Inventory>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _inventoryRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReserveAsync_WithEnoughStock_ReservesInventory()
    {
        var productId = Guid.NewGuid();

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            AvailableQuantity = 10,
            ReservedQuantity = 2,
            UpdatedAt = DateTime.UtcNow
        };

        _inventoryRepository
            .Setup(repository => repository.GetTrackedByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var result = await _inventoryService.ReserveAsync(
            productId,
            4);

        Assert.NotNull(result);
        Assert.Equal(6, result.AvailableQuantity);
        Assert.Equal(6, result.ReservedQuantity);

        _inventoryRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReserveAsync_WithInsufficientStock_ThrowsInvalidOperationException()
    {
        var productId = Guid.NewGuid();

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            AvailableQuantity = 3,
            ReservedQuantity = 0
        };

        _inventoryRepository
            .Setup(repository => repository.GetTrackedByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _inventoryService.ReserveAsync(
                    productId,
                    5));

        Assert.Equal(
            "Insufficient inventory.",
            exception.Message);

        Assert.Equal(3, inventory.AvailableQuantity);
        Assert.Equal(0, inventory.ReservedQuantity);

        _inventoryRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveAsync_WithInvalidQuantity_ThrowsArgumentException()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _inventoryService.ReserveAsync(
                    Guid.NewGuid(),
                    0));

        Assert.Equal(
            "Reservation quantity must be greater than zero.",
            exception.Message);

        _inventoryRepository.Verify(
            repository => repository.GetTrackedByProductIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReleaseAsync_WithValidQuantity_ReleasesInventory()
    {
        var productId = Guid.NewGuid();

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            AvailableQuantity = 6,
            ReservedQuantity = 4,
            UpdatedAt = DateTime.UtcNow
        };

        _inventoryRepository
            .Setup(repository => repository.GetTrackedByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var result = await _inventoryService.ReleaseAsync(
            productId,
            3);

        Assert.NotNull(result);
        Assert.Equal(9, result.AvailableQuantity);
        Assert.Equal(1, result.ReservedQuantity);

        _inventoryRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReleaseAsync_WhenQuantityExceedsReserved_ThrowsInvalidOperationException()
    {
        var productId = Guid.NewGuid();

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            AvailableQuantity = 6,
            ReservedQuantity = 2
        };

        _inventoryRepository
            .Setup(repository => repository.GetTrackedByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _inventoryService.ReleaseAsync(
                    productId,
                    3));

        Assert.Equal(
            "Cannot release more inventory than is currently reserved.",
            exception.Message);

        _inventoryRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveAsync_WhenConcurrencyConflictOccurs_ThrowsInventoryConcurrencyException()
    {
        var productId = Guid.NewGuid();

        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            AvailableQuantity = 10,
            ReservedQuantity = 0
        };

        _inventoryRepository
            .Setup(repository => repository.GetTrackedByProductIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        _inventoryRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new DbUpdateConcurrencyException());

        await Assert.ThrowsAsync<InventoryConcurrencyException>(
            () => _inventoryService.ReserveAsync(
                productId,
                2));
    }
}