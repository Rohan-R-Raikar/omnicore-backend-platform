using OmniCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        // Order this item belong to
        public Guid OrderId { get; set; }

        public Order Order { get; set; } = null!;

        // Product being ordered
        public Guid ProductId { get; set; }

        public Product Product { get; set; } = null!;

        // Quantity of product ordered
        public int Quantity { get; set; }

        // Price of the product at order time
        public decimal UnitPrice { get; set; }
    }
}
