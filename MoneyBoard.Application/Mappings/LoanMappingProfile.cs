using AutoMapper;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Application.Mappings
{
    public class LoanMappingProfile : BaseMappingProfile
    {
        protected override void CreateMaps()
        {
            CreateMap<CreateLoanDto, Loan>()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Repayments, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => LoanStatus.Active));

            CreateMap<Loan, LoanDetailsDto>();

            CreateMap<UpdateLoanDto, Loan>()
                .ForMember(dest => dest.Principal, opt => opt.Ignore())
                .ForMember(dest => dest.InterestType, opt => opt.Ignore())
                .ForMember(dest => dest.StartDate, opt => opt.Ignore())
                .ForMember(dest => dest.Currency, opt => opt.Ignore());
        }
    }
}