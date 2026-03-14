using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Application.DTOs.Order
{
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();
        public DateTime CreatedAt { get; set; }
    }
}
