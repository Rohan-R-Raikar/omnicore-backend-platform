using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Application.DTOs.Orders
{
    public class CreateOrderRequest
    {
        public Guid CustomerId { get; set; }

        public List<CreateOrderItemRequest> Items { get; set; } = new List<CreateOrderItemRequest>();
    }

    public class CreateOrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
