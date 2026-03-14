using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Application.DTOs.Product
{
    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public Guid CategoryId { get; set; }
    }
}
