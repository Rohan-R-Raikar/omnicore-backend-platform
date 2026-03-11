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
        public Guid UserId { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public User? User { get; set; }
    }
}
