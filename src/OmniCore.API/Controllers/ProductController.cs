using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmniCore.Application.DTOs.Product;
using OmniCore.Application.Interfaces;
using System.Security.Claims;

namespace OmniCore.API.Controllers
{
    [ApiController]
    [Route("api/products")]
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
            var sellerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.CreateAsync(request, sellerId));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var product = await _service.GetByIdAsync(id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateProductRequest request)
        {
            await _service.UpdateAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
