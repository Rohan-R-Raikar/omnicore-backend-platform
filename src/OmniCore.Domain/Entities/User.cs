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
        // User login email (unique)
        public string Email { get; set; } = string.Empty;

        // Hashed password for authentication
        public string PasswordHash { get; set; } = string.Empty;

        // Display name of the user
        public string FullName { get; set; } = string.Empty;
        
        // Foreign key to Role
        public Guid RoleId { get; set; }
        
        // Navigation property to Role
        public Role Role { get; set; } = null!;
        
        // Idicates if user account is active
        public bool IsActive { get; set; } = true;
        
        // User refresh tokens for JWT authentication
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // Notifications sent to the user
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        // Products created by the user (Seller)
        public ICollection<Product> Products { get; set; } = new List<Product>();

        // Orders placed by the user (Customer)
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
