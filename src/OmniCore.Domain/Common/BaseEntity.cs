using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Domain.Common
{
    public abstract class BaseEntity
    {
        // Primary key for all entities
        public Guid Id { get; set; } = Guid.NewGuid();

        // When the record was craeted
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // When the record was last updated
        public DateTime? UpdatedAt { get; set; }
        
        // Soft delete timestamp
        public DateTime? DeletedAt { get; set; }

        // Soft delete flag (used with global query filters)
        public bool IsDeleted { get; set; } = false;
    }
}
