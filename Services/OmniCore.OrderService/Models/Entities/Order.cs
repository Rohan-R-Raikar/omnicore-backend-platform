using OmniCore.OrderService.Models.Enums;
using System;
using System.Collections.Generic;

namespace OmniCore.OrderService.Models.Entities;

public class Order
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
}