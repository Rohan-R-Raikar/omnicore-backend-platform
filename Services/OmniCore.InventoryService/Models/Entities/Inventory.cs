namespace OmniCore.InventoryService.Models.Entities;

public class Inventory
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int AvailableQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}