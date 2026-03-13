using Microsoft.EntityFrameworkCore;
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

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> PlaceOrderAsync(Guid customerId, CreateOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var productIds = request.Items.Select(i => i.ProductId).ToList();

                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                if (products.Count != request.Items.Count)
                    throw new Exception("Some products not found");

                decimal totalPrice = 0;

                var orderItems = new List<OrderItem>();

                foreach (var item in request.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);

                    if (product.Stock < item.Quantity)
                        throw new Exception($"Not enough stock for product {product.Name}");

                    product.Stock -= item.Quantity;

                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    };

                    totalPrice += product.Price * item.Quantity;

                    orderItems.Add(orderItem);
                }

                var order = new Order
                {
                    CustomerId = customerId,
                    TotalPrice = totalPrice,
                    Status = OrderStatus.Pending,
                    Items = orderItems
                };

                _context.Orders.Add(order);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return order.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
