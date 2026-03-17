using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniCore.Application.DTOs.Common;
using OmniCore.Application.DTOs.Product;
using OmniCore.Application.Interfaces;
using OmniCore.Domain.Entities;
using OmniCore.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ProductResponse> CreateAsync(CreateProductRequest request, Guid sellerId)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(x => x.Id == request.CategoryId);

                if (category == null)
                    throw new Exception("Category not found");

                var product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    CategoryId = request.CategoryId,
                    SellerId = sellerId
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    CategoryId = product.CategoryId,
                    CategoryName = category.Name,
                    IsActive = product.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<List<ProductResponse>> GetAllAsync()
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Select(p => new ProductResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Stock = p.Stock,
                        IsActive = p.IsActive,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                throw;
            }
        }

        public async Task<ProductResponse?> GetByIdAsync(Guid id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (product == null)
                    return null;

                return new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    CategoryId = product.CategoryId,
                    CategoryName = product.Category.Name,
                    IsActive = product.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {Id}", id);
                throw;
            }
        }

        public async Task UpdateAsync(Guid id, UpdateProductRequest request)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (product == null)
                    throw new Exception("Product not found");

                product.Name = request.Name;
                product.Description = request.Description;
                product.Price = request.Price;
                product.Stock = request.Stock;
                product.IsActive = request.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {Id}", id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (product == null)
                    throw new Exception("Product not found");

                product.IsDeleted = true;
                product.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync(QueryParams query)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();

                products = products.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.Category.Name.ToLower().Contains(search));
            }

            products = query.SortBy?.ToLower() switch
            {
                "price" => query.SortOrder == "desc"
                    ? products.OrderByDescending(p => p.Price)
                    : products.OrderBy(p => p.Price),

                "name" => query.SortOrder == "desc"
                    ? products.OrderByDescending(p => p.Name)
                    : products.OrderBy(p => p.Name),

                _ => products.OrderBy(p => p.Id)
            };

            products = products
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize);

            return await products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive,

                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,

                SellerId = p.SellerId,
                SellerName = p.Seller.FullName
            }).ToListAsync();
        }
    }
}
