using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using OmniCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Domain.Entities
{
    public class Category : BaseEntity
    {
        // Category name
        public string Name { get; set; } = string.Empty;
        
        // Optional category description
        public string? Description { get; set; }

        // Indicates if category is visible / active
        public bool IsActive { get; set; } = true;

        // Products belonging to this category
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
