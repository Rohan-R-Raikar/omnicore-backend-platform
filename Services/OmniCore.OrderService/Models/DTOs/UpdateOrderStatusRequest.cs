using System.ComponentModel.DataAnnotations;

namespace OmniCore.OrderService.Models.DTOs;

public class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}