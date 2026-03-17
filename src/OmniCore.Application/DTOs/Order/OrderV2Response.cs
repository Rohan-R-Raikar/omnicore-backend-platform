using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Application.DTOs.Order
{
    public class OrderV2Response
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalItems { get; set; }
    }
}
