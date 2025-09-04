using AutoMapper;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Application.Mappings
{
    public class RepaymentMappingProfile : BaseMappingProfile
    {
        protected override void CreateMaps()
        {
            CreateMap<Repayment, RepaymentDto>()
                .ForMember(d => d.InterestComponent, o => o.MapFrom(s => s.InterestComponent))
                .ForMember(d => d.PrincipalComponent, o => o.MapFrom(s => s.PrincipalComponent));

            CreateMap<Repayment, RepaymentResponseDto>()
                .IncludeBase<Repayment, RepaymentDto>();
        }
    }
}