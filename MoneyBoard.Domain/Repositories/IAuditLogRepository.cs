using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Domain.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAuditLogAsync(AuditLog auditLog);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page, int pageSize, string? entityType = null, Guid? changedBy = null);
        Task<int> GetAuditLogCountAsync(string? entityType = null, Guid? changedBy = null);
        Task SaveChangesAsync();
    }
}
