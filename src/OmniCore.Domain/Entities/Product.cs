using OmniCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Domain.Entities
{
    public class Product : BaseEntity
    {
        // Product name
        public string Name { get; set; } = string.Empty;

        // Product description
        public string Description { get; set; } = string.Empty;

        // Product price
        public decimal Price { get; set; }

        // Product stock quantity
        public int Stock { get; set; }

        // Indicates if product is active
        public bool IsActive { get; set; } = true;

        // Indicates if product is active
        public Guid SellerId { get; set; }

        public User Seller { get; set; } = null!;

        // Product category
        public Guid CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        // Order items containing this product
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
