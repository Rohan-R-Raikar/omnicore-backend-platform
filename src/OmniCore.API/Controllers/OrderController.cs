using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OmniCore.Application.DTOs.Order;
using OmniCore.Application.DTOs.Orders;
using OmniCore.Application.Interfaces;
using System.Security.Claims;

namespace OmniCore.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrderController(IOrderService service)
        {
            _service = service;
        }

        // Moderate (prevent spam orders)
        [HttpPost]
        [EnableRateLimiting("orderPolicy")]
        public async Task<IActionResult> Create(CreateOrderRequest request)
        {
            return Ok(await _service.CreateOrderAsync(request));
        }

        // V1 - Basic
        [HttpGet("{id}")]
        [MapToApiVersion("1.0")]
        [EnableRateLimiting("relaxedPolicy")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetByIdV1(Guid id)
        {
            Response.Headers["Cache-Control"] = "public,max-age=60";

            var order = await _service.GetByIdAsync(id);
            if (order == null) return NotFound();

            return Ok(order);
        }

        // V2 - Enhanced Response
        [HttpGet("{id}")]
        [MapToApiVersion("2.0")]
        [EnableRateLimiting("relaxedPolicy")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetByIdV2(Guid id)
        {
            Response.Headers["Cache-Control"] = "public,max-age=60";

            var order = await _service.GetByIdAsync(id);
            if (order == null) return NotFound();

            var result = new OrderV2Response
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                TotalItems = order.Items?.Count ?? 0
            };

            return Ok(result);
        }

        /*
        // Relaxed (read operation)
        [HttpGet("{id}")]
        [EnableRateLimiting("relaxedPolicy")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetById(Guid id)
        {
            Response.Headers["Cache-Control"] = "public,max-age=60";

            var order = await _service.GetByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }
        */

        // Moderate
        [HttpGet("customer/{customerId}")]
        [EnableRateLimiting("orderPolicy")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetByCustomer(Guid customerId)
        {
            Response.Headers["Cache-Control"] = "public,max-age=60";

            var orders = await _service.GetAllByCustomerAsync(customerId);
            return Ok(orders);
        }

        // Moderate (important action)
        [HttpPost("{id}/cancel")]
        [EnableRateLimiting("orderPolicy")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _service.CancelOrderAsync(id);
            return NoContent();
        }
    }
}
