using OmniCore.Domain.Common;
using OmniCore.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Domain.Entities
{
    public class Order : BaseEntity
    {
        // Customer who placed the order
        public Guid CustomerId { get; set; }

        public User Customer { get; set; } = null!;

        // Total price of the order (sum of order items)
        public decimal TotalPrice { get; set; }

        // Current order status
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Products included in this order
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
