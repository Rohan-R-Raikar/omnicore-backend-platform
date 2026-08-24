using System;

namespace OmniCore.OrderService.Models.External;

public class ProductDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}