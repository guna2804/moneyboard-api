using AutoMapper;
using Microsoft.Extensions.Logging;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;

namespace MoneyBoard.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IAuditLogRepository auditLogRepository,
            IMapper mapper,
            ILogger<AuditService> logger)
        {
            _auditLogRepository = auditLogRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task LogAuditAsync(string entityType, string entityId, string action, Guid changedBy, string details)
        {
            _logger.LogInformation("Logging audit: {EntityType} {EntityId} {Action} by {UserId}",
                entityType, entityId, action, changedBy);

            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                ChangedBy = changedBy,
                Details = details
            };

            await _auditLogRepository.AddAuditLogAsync(auditLog);
            await _auditLogRepository.SaveChangesAsync();

            _logger.LogInformation("Audit log created successfully for {EntityType} {EntityId}", entityType, entityId);
        }

        public async Task<PagedAuditLogResponseDto> GetAuditLogsAsync(int page, int pageSize, string? entityType = null, Guid? changedBy = null)
        {
            _logger.LogInformation("Getting audit logs, page: {Page}, size: {Size}", page, pageSize);

            var auditLogs = await _auditLogRepository.GetAuditLogsAsync(page, pageSize, entityType, changedBy);
            var totalCount = await _auditLogRepository.GetAuditLogCountAsync(entityType, changedBy);

            var auditLogDtos = _mapper.Map<IEnumerable<AuditLogDto>>(auditLogs);

            return new PagedAuditLogResponseDto
            {
                AuditLogs = auditLogDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
