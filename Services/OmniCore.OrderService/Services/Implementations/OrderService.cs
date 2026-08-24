using OmniCore.OrderService.Models.DTOs;
using OmniCore.OrderService.Models.Entities;
using OmniCore.OrderService.Models.Enums;
using OmniCore.OrderService.Repositories.Interfaces;
using OmniCore.OrderService.Services.Clients;
using OmniCore.OrderService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniCore.OrderService.Messaging.Producers;
using OmniCore.Shared.Events;

namespace OmniCore.OrderService.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductServiceClient _productServiceClient;
    private readonly IInventoryServiceClient _inventoryServiceClient;
    private readonly ILogger<OrderService> _logger;
    private readonly IOrderEventPublisher _orderEventPublisher;

    public OrderService(
        IOrderRepository orderRepository,
        IProductServiceClient productServiceClient,
        IInventoryServiceClient inventoryServiceClient,
        IOrderEventPublisher orderEventPublisher,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productServiceClient = productServiceClient;
        _inventoryServiceClient = inventoryServiceClient;
        _orderEventPublisher = orderEventPublisher;
        _logger = logger;
    }

    public async Task<OrderResponse?> CancelAsync(
        Guid orderId,
        Guid currentUserId,
        bool isAdmin,
        string authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetTrackedByIdAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (!isAdmin && order.UserId != currentUserId)
        {
            return null;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Order is already cancelled.");
        }

        if (order.Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException(
                "A completed order cannot be cancelled.");
        }

        var releasedItems = new List<(Guid ProductId, int Quantity)>();

        try
        {
            foreach (var item in order.Items)
            {
                var inventory =
                    await _inventoryServiceClient.ReleaseAsync(
                        item.ProductId,
                        item.Quantity,
                        authorizationHeader,
                        cancellationToken);

                if (inventory is null)
                {
                    throw new InvalidOperationException(
                        $"Inventory was not found for product {item.ProductId}.");
                }

                releasedItems.Add(
                    (item.ProductId, item.Quantity));
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Order {OrderNumber} cancelled.",
                order.OrderNumber);

            return MapOrder(order);
        }
        catch
        {
            await RestoreReleasedInventoryAsync(
                releasedItems,
                authorizationHeader,
                cancellationToken);

            throw;
        }
    }

    private async Task RestoreReleasedInventoryAsync(
        IEnumerable<(Guid ProductId, int Quantity)> releasedItems,
        string authorizationHeader,
        CancellationToken cancellationToken)
    {
        foreach (var item in releasedItems.Reverse())
        {
            try
            {
                await _inventoryServiceClient.ReserveAsync(
                    item.ProductId,
                    item.Quantity,
                    authorizationHeader,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to restore inventory reservation for ProductId {ProductId}.",
                    item.ProductId);
            }
        }
    }

    public async Task<OrderResponse?> UpdateStatusAsync(
    Guid orderId,
    string status,
    CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetTrackedByIdAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (!Enum.TryParse<OrderStatus>(
                status,
                true,
                out var newStatus))
        {
            throw new InvalidOperationException(
                "Invalid order status.");
        }

        if (newStatus == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Use the cancellation endpoint to cancel an order.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "A cancelled order cannot change status.");
        }

        if (order.Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException(
                "A completed order cannot change status.");
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepository.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} status changed to {Status}.",
            order.OrderNumber,
            newStatus);

        return MapOrder(order);
    }

    public async Task<OrderResponse> CreateAsync(
        Guid userId,
        CreateOrderRequest request,
        string authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException(
                "Order must contain at least one item.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var reservedItems = new List<(Guid ProductId, int Quantity)>();

        try
        {
            foreach (var requestItem in request.Items)
            {
                var product =
                    await _productServiceClient.GetProductAsync(
                        requestItem.ProductId,
                        authorizationHeader,
                        cancellationToken);

                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Product {requestItem.ProductId} was not found.");
                }

                if (!product.IsActive)
                {
                    throw new InvalidOperationException(
                        $"Product {product.Name} is inactive and cannot be ordered.");
                }

                var inventory =
                    await _inventoryServiceClient.ReserveAsync(
                        product.Id,
                        requestItem.Quantity,
                        authorizationHeader,
                        cancellationToken);

                if (inventory is null)
                {
                    throw new InvalidOperationException(
                        $"Inventory was not found for product {product.Name}.");
                }

                reservedItems.Add(
                    (product.Id, requestItem.Quantity));

                var totalPrice =
                    product.Price * requestItem.Quantity;

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = requestItem.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = totalPrice
                });
            }

            order.TotalAmount =
                order.Items.Sum(item => item.TotalPrice);

            await _orderRepository.AddAsync(
                order,
                cancellationToken);

            await _orderRepository.SaveChangesAsync(
                cancellationToken);

            await _orderEventPublisher.PublishOrderCreatedAsync(
                new OrderCreatedEvent
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = order.UserId,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt
                },
                cancellationToken);

            _logger.LogInformation(
                "Order {OrderNumber} created for UserId {UserId}. Total: {TotalAmount}",
                order.OrderNumber,
                userId,
                order.TotalAmount);

            return MapOrder(order);
        }
        catch
        {
            await ReleaseReservedInventoryAsync(
                reservedItems,
                authorizationHeader,
                cancellationToken);

            throw;
        }
    }

    public async Task<OrderResponse?> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (!isAdmin && order.UserId != currentUserId)
        {
            return null;
        }

        return MapOrder(order);
    }

    public async Task<IReadOnlyCollection<OrderResponse>> GetOrdersAsync(
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var orders = isAdmin
            ? await _orderRepository.GetAllAsync(cancellationToken)
            : await _orderRepository.GetByUserIdAsync(
                currentUserId,
                cancellationToken);

        return orders
            .Select(MapOrder)
            .ToList();
    }

    private async Task ReleaseReservedInventoryAsync(
        IEnumerable<(Guid ProductId, int Quantity)> reservedItems,
        string authorizationHeader,
        CancellationToken cancellationToken)
    {
        foreach (var item in reservedItems.Reverse())
        {
            try
            {
                await _inventoryServiceClient.ReleaseAsync(
                    item.ProductId,
                    item.Quantity,
                    authorizationHeader,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to compensate inventory reservation for ProductId {ProductId}.",
                    item.ProductId);
            }
        }
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32];
    }

    private static OrderResponse MapOrder(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items
                .Select(item => new OrderItemResponse
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                })
                .ToList()
        };
    }
}