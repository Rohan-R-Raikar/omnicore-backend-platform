using OmniCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        
        public Guid RoleId { get; set; }
        
        public Role Role { get; set; } = null!;
        
        public bool IsActive { get; set; } = true;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public ICollection<Product> Products { get; set; } = new List<Product>();

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
