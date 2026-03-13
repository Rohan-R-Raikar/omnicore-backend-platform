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
        public Guid CustomerId { get; set; }

        public User Customer { get; set; } = null!;

        public decimal TotalPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
