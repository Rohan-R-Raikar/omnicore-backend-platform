using System;
using System.ComponentModel.DataAnnotations;

namespace OmniCore.OrderService.Models.DTOs;

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}