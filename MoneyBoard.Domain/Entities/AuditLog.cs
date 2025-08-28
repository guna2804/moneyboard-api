using MoneyBoard.Domain.Common;

namespace MoneyBoard.Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty; // String to handle different ID types if needed
        public string Action { get; set; } = string.Empty; // e.g., "Create", "Update", "Delete"
        public Guid ChangedBy { get; set; } // UserId
        public string Details { get; set; } = string.Empty; // JSON string for old/new values
    }
}
