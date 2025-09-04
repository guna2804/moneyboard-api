using Microsoft.EntityFrameworkCore;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Infrastructure.Data
{
    public class AuditLogRepository(AppDbContext context) : IAuditLogRepository
    {
        public async Task AddAuditLogAsync(AuditLog auditLog)
        {
            await context.AuditLogs.AddAsync(auditLog);
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page, int pageSize, string? entityType = null, Guid? changedBy = null)
        {
            var query = context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (changedBy.HasValue)
            {
                query = query.Where(a => a.ChangedBy == changedBy.Value);
            }

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetAuditLogCountAsync(string? entityType = null, Guid? changedBy = null)
        {
            var query = context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (changedBy.HasValue)
            {
                query = query.Where(a => a.ChangedBy == changedBy.Value);
            }

            return await query.CountAsync();
        }

        public Task SaveChangesAsync() => context.SaveChangesAsync();
    }
}
