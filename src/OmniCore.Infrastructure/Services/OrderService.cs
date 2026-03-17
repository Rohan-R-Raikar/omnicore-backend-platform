using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniCore.Application.DTOs.Order;
using OmniCore.Application.DTOs.Orders;
using OmniCore.Application.Interfaces;
using OmniCore.Domain.Entities;
using OmniCore.Domain.Enums;
using OmniCore.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IMemoryCache _cache;

        public OrderService(ApplicationDbContext context, 
                            ILogger<OrderService> logger,
                            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var customer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.CustomerId);

                if (customer == null)
                    throw new Exception("Customer not found");

                if (request.Items.Count == 0)
                    throw new Exception("Order must have at least one item");

                var order = new Order
                {
                    CustomerId = request.CustomerId,
                    Status = OrderStatus.Pending,
                    TotalPrice = 0
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                decimal totalPrice = 0;

                foreach (var itemReq in request.Items)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == itemReq.ProductId && p.IsActive);

                    if (product == null)
                        throw new Exception($"Product {itemReq.ProductId} not found");

                    if (product.Stock < itemReq.Quantity)
                        throw new Exception($"Insufficient stock for product {product.Name}");

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = itemReq.Quantity,
                        UnitPrice = product.Price
                    };

                    _context.OrderItems.Add(orderItem);

                    product.Stock -= itemReq.Quantity; // reduce stock
                    totalPrice += orderItem.Quantity * orderItem.UnitPrice;
                }

                order.TotalPrice = totalPrice;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _cache.Remove($"orders_customer_{request.CustomerId}");

                return new OrderResponse
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    TotalPrice = order.TotalPrice,
                    Status = order.Status.ToString(),
                    Items = await _context.OrderItems
                        .Where(oi => oi.OrderId == order.Id)
                        .Include(oi => oi.Product)
                        .Select(oi => new OrderItemResponse
                        {
                            ProductId = oi.ProductId,
                            ProductName = oi.Product.Name,
                            UnitPrice = oi.UnitPrice,
                            Quantity = oi.Quantity
                        }).ToListAsync(),
                    CreatedAt = order.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for customer {CustomerId}", request.CustomerId);
                throw;
            }
        }

        public async Task<OrderResponse?> GetByIdAsync(Guid orderId)
        {
            var cacheKey = $"order_{orderId}";

            if (_cache.TryGetValue(cacheKey, out OrderResponse cachedOrder))
            {
                return cachedOrder;
            }
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return null;

                var result = new OrderResponse
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    TotalPrice = order.TotalPrice,
                    Status = order.Status.ToString(),
                    CreatedAt = order.CreatedAt,
                    Items = order.Items.Select(oi => new OrderItemResponse
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity
                    }).ToList()
                };

                //_cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<List<OrderResponse>> GetAllByCustomerAsync(Guid customerId)
        {
            var cacheKey = $"orders_customer_{customerId}";

            if (_cache.TryGetValue(cacheKey, out List<OrderResponse> cachedOrders))
            {
                return cachedOrders;
            }

            try
            {
                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customerId)
                    .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                    .ToListAsync();

                var result = orders.Select(order => new OrderResponse
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    TotalPrice = order.TotalPrice,
                    Status = order.Status.ToString(),
                    CreatedAt = order.CreatedAt,
                    Items = order.Items.Select(oi => new OrderItemResponse
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity
                    }).ToList()
                }).ToList();

                //_cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for customer {CustomerId}", customerId);
                throw;
            }
        }

        public async Task CancelOrderAsync(Guid orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    throw new Exception("Order not found");

                if (order.Status != OrderStatus.Pending)
                    throw new Exception("Only pending orders can be cancelled");

                // Revert stock
                foreach (var item in order.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                    }
                }

                order.Status = OrderStatus.Cancelled;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _cache.Remove($"order_{orderId}");
                _cache.Remove($"orders_customer_{order.CustomerId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                throw;
            }
        }
    }
}
