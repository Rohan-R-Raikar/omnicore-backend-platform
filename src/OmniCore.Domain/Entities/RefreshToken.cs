using OmniCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        // Foreign key to use the user who owns this refresh token
        public Guid UserId { get; set; }

        // The actual refresh token string stored in DB
        public string Token { get; set; } = string.Empty;

        // Expiration time of the refresh token
        public DateTime ExpiresAt { get; set; }

        // Indicates if the token has been revoked (logout or security)
        public bool IsRevoked { get; set; }

        // Timestamp when the token was revoked
        public DateTime? RevokedAt { get; set; }

        // Navigation property to Users
        public User User { get; set; } = null!;
    }
}
