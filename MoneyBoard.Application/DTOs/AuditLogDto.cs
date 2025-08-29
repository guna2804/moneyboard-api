using System;

namespace MoneyBoard.Application.DTOs
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Guid ChangedBy { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}
