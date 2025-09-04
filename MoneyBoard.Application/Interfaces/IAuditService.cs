using MoneyBoard.Application.DTOs;

namespace MoneyBoard.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAuditAsync(string entityType, string entityId, string action, Guid changedBy, string details);
        Task<PagedAuditLogResponseDto> GetAuditLogsAsync(int page, int pageSize, string? entityType = null, Guid? changedBy = null);
    }
}
