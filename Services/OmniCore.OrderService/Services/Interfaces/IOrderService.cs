using OmniCore.OrderService.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.OrderService.Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(
        Guid userId,
        CreateOrderRequest request,
        string authorizationHeader,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OrderResponse>> GetOrdersAsync(
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> CancelAsync(
        Guid orderId,
        Guid currentUserId,
        bool isAdmin,
        string authorizationHeader,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> UpdateStatusAsync(
        Guid orderId,
        string status,
        CancellationToken cancellationToken = default);
}