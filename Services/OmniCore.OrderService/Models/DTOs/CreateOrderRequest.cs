using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmniCore.OrderService.Models.DTOs;

public class CreateOrderRequest
{
    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}