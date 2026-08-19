using OmniCore.ProductService.Models.DTOs;
using OmniCore.ProductService.Models.Entities;
using OmniCore.ProductService.Repositories.Interfaces;
using OmniCore.ProductService.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using OmniCore.ProductService.Caching;

namespace OmniCore.ProductService.Services.Implementations;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;

    public ProductService(
        IProductRepository productRepository,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
    }

    public async Task<PagedResult<ProductResponse>> GetProductsAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetProductListCacheKey(query);

        var cachedResult =
            await _cacheService.GetAsync<PagedResult<ProductResponse>>(
                cacheKey,
                cancellationToken);

        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var (items, totalCount) =
            await _productRepository.GetPagedAsync(
                query,
                cancellationToken);

        var result = new PagedResult<ProductResponse>
        {
            Items = items.Select(MapProduct).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(
                totalCount / (double)query.PageSize)
        };

        await _cacheService.SetAsync(
            cacheKey,
            result,
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetProductCacheKey(id);

        var cachedProduct =
            await _cacheService.GetAsync<ProductResponse>(
                cacheKey,
                cancellationToken);

        if (cachedProduct is not null)
        {
            return cachedProduct;
        }

        var product = await _productRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        var response = MapProduct(product);

        await _cacheService.SetAsync(
            cacheKey,
            response,
            cancellationToken: cancellationToken);

        return response;
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedSku = request.SKU
            .Trim()
            .ToUpperInvariant();

        var skuExists = await _productRepository.SkuExistsAsync(
            normalizedSku,
            cancellationToken: cancellationToken);

        if (skuExists)
        {
            throw new InvalidOperationException(
                "A product with this SKU already exists.");
        }

        var now = DateTime.UtcNow;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            SKU = normalizedSku,
            Description = request.Description.Trim(),
            Price = request.Price,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _productRepository.AddAsync(
            product,
            cancellationToken);

        await _productRepository.SaveChangesAsync(
            cancellationToken);

        await _cacheService.RemoveByPrefixAsync(
            "products:list:",
            cancellationToken);

        return MapProduct(product);
    }

    public async Task<ProductResponse?> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _productRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (existing is null)
        {
            return null;
        }

        var normalizedSku = request.SKU
            .Trim()
            .ToUpperInvariant();

        var skuExists = await _productRepository.SkuExistsAsync(
            normalizedSku,
            id,
            cancellationToken);

        if (skuExists)
        {
            throw new InvalidOperationException(
                "A product with this SKU already exists.");
        }

        existing.Name = request.Name.Trim();
        existing.SKU = normalizedSku;
        existing.Description = request.Description.Trim();
        existing.Price = request.Price;
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(existing);

        await _productRepository.SaveChangesAsync(
            cancellationToken);

        await _cacheService.RemoveAsync(
            GetProductCacheKey(existing.Id),
            cancellationToken);

        await _cacheService.RemoveByPrefixAsync(
            "products:list:",
            cancellationToken);

        return MapProduct(existing);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var existing = await _productRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (existing is null)
        {
            return false;
        }

        _productRepository.Delete(existing);

        await _productRepository.SaveChangesAsync(
            cancellationToken);

        await _cacheService.RemoveAsync(
            GetProductCacheKey(existing.Id),
            cancellationToken);

        await _cacheService.RemoveByPrefixAsync(
            "products:list:",
            cancellationToken);

        return true;
    }

    private static ProductResponse MapProduct(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            Price = product.Price,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
    private static string GetProductCacheKey(Guid id)
    {
        return $"products:id:{id}";
    }

    private static string GetProductListCacheKey(
        ProductQueryParameters query)
    {
        return $"products:list:" +
               $"{query.Search ?? "all"}:" +
               $"{query.IsActive?.ToString() ?? "all"}:" +
               $"{query.SortBy ?? "default"}:" +
               $"{query.SortDirection ?? "asc"}:" +
               $"{query.PageNumber}:" +
               $"{query.PageSize}";
    }
}