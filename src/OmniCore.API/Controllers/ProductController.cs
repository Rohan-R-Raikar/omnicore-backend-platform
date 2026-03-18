using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmniCore.Application.DTOs.Common;
using OmniCore.Application.DTOs.Product;
using OmniCore.Application.Interfaces;
using OmniCore.Infrastructure.Services;
using System.Security.Claims;

namespace OmniCore.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductController(IProductService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductRequest request)
        {
            //var sellerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            //return Ok(await _service.CreateAsync(request, sellerId));

            var sellerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result =  await _service.CreateAsync(request, sellerId);

            return Ok(new ApiResponse<object>(result));
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(new ApiResponse<object>(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var product = await _service.GetByIdAsync(id);

            if (product == null)
                return NotFound();

            return Ok(new ApiResponse<object>(product));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateProductRequest request)
        {
            await _service.UpdateAsync(id, request);

            return Ok(new ApiResponse<string>(null, "Product Updated successfully"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);

            return Ok(new ApiResponse<string>(null, "Product Deleted successfully"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryParams queryParams)
        {
            var products = await _service.GetAllAsync(queryParams);
            return Ok(new ApiResponse<object>(products));
        }
    }
}
