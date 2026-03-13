using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniCore.Application.DTOs.Category;
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
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ApplicationDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
        {
            try
            {
                _logger.LogInformation("Creating category {Name}", request.Name);

                var category = new Category
                {
                    Name = request.Name,
                    Description = request.Description
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<List<CategoryResponse>> GetAllAsync()
        {
            try
            {
                return await _context.Categories
                    .Select(c => new CategoryResponse
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        IsActive = c.IsActive
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                throw;
            }
        }

        public async Task<CategoryResponse?> GetByIdAsync(Guid id)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    return null;

                return new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category {Id}", id);
                throw;
            }
        }

        public async Task UpdateAsync(Guid id, UpdateCategoryRequest request)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    throw new Exception("Category not found");

                category.Name = request.Name;
                category.Description = request.Description;
                category.IsActive = request.IsActive;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Category updated {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {Id}", id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                    throw new Exception("Category not found");

                category.IsDeleted = true;
                category.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Category soft deleted {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {Id}", id);
                throw;
            }
        }
    }
}
