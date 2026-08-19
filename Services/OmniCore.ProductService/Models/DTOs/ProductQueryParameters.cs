using System.ComponentModel.DataAnnotations;

namespace OmniCore.ProductService.Models.DTOs;

public class ProductQueryParameters
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; } = "asc";

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}