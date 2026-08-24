namespace OmniCore.ProductService.Models.DTOs;

public class ApiErrorResponse
{
    public int Status { get; set; }

    public string Message { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;
}