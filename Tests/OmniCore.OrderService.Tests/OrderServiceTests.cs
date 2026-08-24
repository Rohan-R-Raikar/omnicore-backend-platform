using Microsoft.Extensions.Logging;
using Moq;
using OmniCore.OrderService.Messaging.Producers;
using OmniCore.OrderService.Models.DTOs;
using OmniCore.OrderService.Models.Entities;
using OmniCore.OrderService.Models.Enums;
using OmniCore.OrderService.Models.External;
using OmniCore.OrderService.Repositories.Interfaces;
using OmniCore.OrderService.Services.Clients;
using OmniCore.Shared.Events;

using OrderServiceImpl =
    OmniCore.OrderService.Services.Implementations.OrderService;

namespace OmniCore.OrderService.Tests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository;
    private readonly Mock<IProductServiceClient> _productServiceClient;
    private readonly Mock<IInventoryServiceClient> _inventoryServiceClient;
    private readonly Mock<IOrderEventPublisher> _orderEventPublisher;
    private readonly Mock<ILogger<OrderServiceImpl>> _logger;

    private readonly OrderServiceImpl _orderService;

    public OrderServiceTests()
    {
        _orderRepository = new Mock<IOrderRepository>();
        _productServiceClient = new Mock<IProductServiceClient>();
        _inventoryServiceClient = new Mock<IInventoryServiceClient>();
        _orderEventPublisher = new Mock<IOrderEventPublisher>();
        _logger = new Mock<ILogger<OrderServiceImpl>>();

        _orderService = new OrderServiceImpl(
            _orderRepository.Object,
            _productServiceClient.Object,
            _inventoryServiceClient.Object,
            _orderEventPublisher.Object,
            _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidOrder_CalculatesTotalAndCreatesOrder()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var request = new CreateOrderRequest
        {
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 3
                }
            ]
        };

        var product = new ProductDto
        {
            Id = productId,
            Name = "Mechanical Keyboard",
            SKU = "KEY-001",
            Price = 2000,
            IsActive = true
        };

        _productServiceClient
            .Setup(client => client.GetProductAsync(
                productId,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _inventoryServiceClient
            .Setup(client => client.ReserveAsync(
                productId,
                3,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryDto
            {
                ProductId = productId,
                AvailableQuantity = 7,
                ReservedQuantity = 3
            });

        var result = await _orderService.CreateAsync(
            userId,
            request,
            "Bearer token");

        Assert.Equal(userId, result.UserId);
        Assert.Equal(6000, result.TotalAmount);
        Assert.Equal("Pending", result.Status);
        Assert.Single(result.Items);

        Assert.Equal(2000, result.Items.First().UnitPrice);
        Assert.Equal(6000, result.Items.First().TotalPrice);

        _orderRepository.Verify(
            repository => repository.AddAsync(
                It.Is<Order>(order =>
                    order.UserId == userId &&
                    order.TotalAmount == 6000 &&
                    order.Items.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _orderRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _orderEventPublisher.Verify(
            publisher => publisher.PublishOrderCreatedAsync(
                It.Is<OrderCreatedEvent>(orderEvent =>
                    orderEvent.UserId == userId &&
                    orderEvent.TotalAmount == 6000),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ThrowsAndDoesNotReserveInventory()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var request = new CreateOrderRequest
        {
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 1
                }
            ]
        };

        _productServiceClient
            .Setup(client => client.GetProductAsync(
                productId,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateAsync(
                    userId,
                    request,
                    "Bearer token"));

        Assert.Contains(
            "was not found",
            exception.Message);

        _inventoryServiceClient.Verify(
            client => client.ReserveAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _orderRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenProductInactive_ThrowsAndDoesNotReserveInventory()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var request = new CreateOrderRequest
        {
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 1
                }
            ]
        };

        _productServiceClient
            .Setup(client => client.GetProductAsync(
                productId,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto
            {
                Id = productId,
                Name = "Inactive Product",
                Price = 1000,
                IsActive = false
            });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateAsync(
                    userId,
                    request,
                    "Bearer token"));

        Assert.Contains(
            "inactive",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _inventoryServiceClient.Verify(
            client => client.ReserveAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSecondReservationFails_ReleasesFirstReservation()
    {
        var userId = Guid.NewGuid();

        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        var request = new CreateOrderRequest
        {
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = product1Id,
                    Quantity = 2
                },
                new CreateOrderItemRequest
                {
                    ProductId = product2Id,
                    Quantity = 5
                }
            ]
        };

        _productServiceClient
            .Setup(client => client.GetProductAsync(
                product1Id,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto
            {
                Id = product1Id,
                Name = "Product A",
                Price = 100,
                IsActive = true
            });

        _productServiceClient
            .Setup(client => client.GetProductAsync(
                product2Id,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto
            {
                Id = product2Id,
                Name = "Product B",
                Price = 200,
                IsActive = true
            });

        _inventoryServiceClient
            .Setup(client => client.ReserveAsync(
                product1Id,
                2,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryDto
            {
                ProductId = product1Id
            });

        _inventoryServiceClient
            .Setup(client => client.ReserveAsync(
                product2Id,
                5,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new HttpRequestException(
                    "Inventory reservation failed."));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => _orderService.CreateAsync(
                userId,
                request,
                "Bearer token"));

        _inventoryServiceClient.Verify(
            client => client.ReleaseAsync(
                product1Id,
                2,
                "Bearer token",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _orderRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WithValidOrder_ReleasesInventoryAndCancelsOrder()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            OrderNumber = "ORD-TEST-001",
            Status = OrderStatus.Pending,
            TotalAmount = 5000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = 2,
                    UnitPrice = 2500,
                    TotalPrice = 5000
                }
            ]
        };

        _orderRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _inventoryServiceClient
            .Setup(client => client.ReleaseAsync(
                productId,
                2,
                "Bearer token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryDto
            {
                ProductId = productId
            });

        var result = await _orderService.CancelAsync(
            orderId,
            userId,
            false,
            "Bearer token");

        Assert.NotNull(result);
        Assert.Equal("Cancelled", result.Status);

        _inventoryServiceClient.Verify(
            client => client.ReleaseAsync(
                productId,
                2,
                "Bearer token",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _orderRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WhenCompleted_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            OrderNumber = "ORD-COMPLETE",
            Status = OrderStatus.Completed
        };

        _orderRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CancelAsync(
                    orderId,
                    userId,
                    false,
                    "Bearer token"));

        Assert.Equal(
            "A completed order cannot be cancelled.",
            exception.Message);

        _inventoryServiceClient.Verify(
            client => client.ReleaseAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerOwnsOrder_ReturnsOrder()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _orderRepository
            .Setup(repository => repository.GetByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = orderId,
                UserId = userId,
                OrderNumber = "ORD-001",
                Status = OrderStatus.Pending,
                Items = new List<OrderItem>()
            });

        var result = await _orderService.GetByIdAsync(
            orderId,
            userId,
            false);

        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotOwnOrder_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _orderRepository
            .Setup(repository => repository.GetByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = orderId,
                UserId = ownerId,
                OrderNumber = "ORD-001",
                Status = OrderStatus.Pending,
                Items = new List<OrderItem>()
            });

        var result = await _orderService.GetByIdAsync(
            orderId,
            currentUserId,
            false);

        Assert.Null(result);
    }
}