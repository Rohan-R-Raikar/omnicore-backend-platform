using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniCore.Domain.Enums;
using OmniCore.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Infrastructure.BackgroundJobs
{
    public class OrderCleanupJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderCleanupJob> _logger;

        public OrderCleanupJob(ApplicationDbContext context, ILogger<OrderCleanupJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CancelExpiredOrders()
        {
            try
            {
                var expiryTime = DateTime.UtcNow.AddMinutes(-30);

                var expiredOrders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < expiryTime)
                    .ToListAsync();

                foreach (var order in expiredOrders)
                {
                    foreach (var item in order.Items)
                    {
                        var product = await _context.Products
                            .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                        if (product != null)
                        {
                            product.Stock += item.Quantity;
                        }
                    }

                    order.Status = OrderStatus.Cancelled;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Expired orders cleanup completed. Cancelled {Count} orders", expiredOrders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning expired orders");
                throw;
            }
        }
    }
}
