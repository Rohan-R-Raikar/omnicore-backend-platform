using System.ComponentModel.DataAnnotations;

namespace OmniCore.InventoryService.Models.DTOs;

public class UpdateInventoryRequest
{
    [Range(0, int.MaxValue)]
    public int AvailableQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReservedQuantity { get; set; }
}