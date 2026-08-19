using OmniCore.ProductService.Models.DTOs;
using OmniCore.ProductService.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.ProductService.Repositories.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Product?> GetBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(
        string sku,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetPagedAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);

    void Update(Product product);

    void Delete(Product product);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}