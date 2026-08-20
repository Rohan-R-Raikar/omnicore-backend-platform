using System.ComponentModel.DataAnnotations;

namespace OmniCore.InventoryService.Models.DTOs;

public class ReserveInventoryRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}