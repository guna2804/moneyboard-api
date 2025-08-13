using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBoard.Domain.Entities
{
    public class AuditLog
    {
        public Guid AuditId { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; } // String to handle different ID types if needed
        public string Action { get; set; } // e.g., "Create", "Update", "Delete"
        public Guid ChangedBy { get; set; } // UserId
        public DateTime ChangedAt { get; set; }
        public string Details { get; set; } // JSON string for old/new values
    }
}