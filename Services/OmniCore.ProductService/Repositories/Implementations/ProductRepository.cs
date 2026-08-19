using Microsoft.EntityFrameworkCore;
using OmniCore.ProductService.Data;
using OmniCore.ProductService.Models.DTOs;
using OmniCore.ProductService.Models.Entities;
using OmniCore.ProductService.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.ProductService.Repositories.Implementations;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.SKU == sku,
                cancellationToken);
    }

    public async Task<bool> SkuExistsAsync(
        string sku,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(product => product.SKU == sku);

        if (excludeProductId.HasValue)
        {
            query = query.Where(
                product => product.Id != excludeProductId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetPagedAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var products = _context.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            products = products.Where(product =>
                product.Name.Contains(search) ||
                product.SKU.Contains(search));
        }

        if (query.IsActive.HasValue)
        {
            products = products.Where(
                product => product.IsActive == query.IsActive.Value);
        }

        products = ApplySorting(products, query);

        var totalCount = await products.CountAsync(cancellationToken);

        var items = await products
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public void Delete(Product product)
    {
        _context.Products.Remove(product);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Product> ApplySorting(
        IQueryable<Product> products,
        ProductQueryParameters query)
    {
        var sortBy = query.SortBy?.Trim().ToLowerInvariant();
        var descending =
            string.Equals(
                query.SortDirection,
                "desc",
                StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "price" => descending
                ? products.OrderByDescending(product => product.Price)
                : products.OrderBy(product => product.Price),

            "name" => descending
                ? products.OrderByDescending(product => product.Name)
                : products.OrderBy(product => product.Name),

            _ => products.OrderBy(product => product.Name)
        };
    }
}