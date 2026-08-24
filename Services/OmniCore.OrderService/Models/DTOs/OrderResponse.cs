using System;
using System.Collections.Generic;

namespace OmniCore.OrderService.Models.DTOs;

public class OrderResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<OrderItemResponse> Items { get; set; }
        = Array.Empty<OrderItemResponse>();
}