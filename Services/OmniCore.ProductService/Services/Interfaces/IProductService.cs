using OmniCore.ProductService.Models.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.ProductService.Services.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetProductsAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductResponse?> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}