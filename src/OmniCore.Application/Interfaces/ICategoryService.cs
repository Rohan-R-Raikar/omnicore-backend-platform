using OmniCore.Application.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);

        Task<List<CategoryResponse>> GetAllAsync();

        Task<CategoryResponse?> GetByIdAsync(Guid id);

        Task UpdateAsync(Guid id, UpdateCategoryRequest request);

        Task DeleteAsync(Guid id);
    }
}
