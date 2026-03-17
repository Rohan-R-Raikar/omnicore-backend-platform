using OmniCore.Application.DTOs.Common;
using OmniCore.Application.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductResponse> CreateAsync(CreateProductRequest request, Guid sellerId);

        Task<List<ProductResponse>> GetAllAsync();

        Task<ProductResponse?> GetByIdAsync(Guid id);

        Task UpdateAsync(Guid id, UpdateProductRequest request);

        Task DeleteAsync(Guid id);

        Task<IEnumerable<ProductDto>> GetAllAsync(QueryParams queryParams);
    }
}
