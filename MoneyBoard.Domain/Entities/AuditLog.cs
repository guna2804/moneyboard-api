using MoneyBoard.Domain.Common;

namespace MoneyBoard.Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public string EntityType { get; set; }
        public string EntityId { get; set; } // String to handle different ID types if needed
        public string Action { get; set; } // e.g., "Create", "Update", "Delete"
        public Guid ChangedBy { get; set; } // UserId
        public string Details { get; set; } // JSON string for old/new values
    }
}