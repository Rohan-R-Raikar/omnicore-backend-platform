using OmniCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Domain.Entities
{
    public class Notification : BaseEntity
    {
        // User who receives the notification
        public Guid UserId { get; set; }

        // Notification message content
        public string Message { get; set; } = string.Empty;

        // Indicates if the user has read the notification
        public bool IsRead { get; set; } = false;

        // Navigation property to User
        public User? User { get; set; }

        // Type/category of notification (Order, System, etc.)
        public string Type { get; set; } = string.Empty;
    }
}
