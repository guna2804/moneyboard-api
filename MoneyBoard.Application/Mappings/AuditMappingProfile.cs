using MoneyBoard.Application.DTOs;
using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Application.Mappings
{
    public class AuditMappingProfile : BaseMappingProfile
    {
        protected override void CreateMaps()
        {
            // Audit log mappings
            CreateMap<AuditLog, AuditLogDto>();
        }
    }
}
