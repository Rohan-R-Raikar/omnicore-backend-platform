using System;

namespace OmniCore.InventoryService.Models.DTOs;

public class InventoryResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int AvailableQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateTime UpdatedAt { get; set; }
}