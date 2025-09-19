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

            CreateMap<Loan, LoanWithRepaymentHistoryDto>()
                .ForMember(dest => dest.RepaymentHistory, opt => opt.Ignore()); // Set manually in service

            // UpdateLoanDto mapping - service layer controls which fields are actually updated
            CreateMap<UpdateLoanDto, Loan>()
                .ForMember(dest => dest.CounterpartyName, opt => opt.MapFrom(src => src.CounterpartyName))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Principal, opt => opt.MapFrom(src => src.Principal))
                .ForMember(dest => dest.InterestRate, opt => opt.MapFrom(src => src.InterestRate))
                .ForMember(dest => dest.InterestType, opt => opt.MapFrom(src => src.InterestType))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.RepaymentFrequency, opt => opt.MapFrom(src => src.RepaymentFrequency))
                .ForMember(dest => dest.AllowOverpayment, opt => opt.MapFrom(src => src.AllowOverpayment))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
                // Ignore navigation properties and system fields
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Repayments, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // AmendLoanDto mapping for handling changes after first repayment that require versioning
            CreateMap<AmendLoanDto, Loan>()
                .ForMember(dest => dest.InterestRate, opt => opt.MapFrom(src => src.InterestRate))
                .ForMember(dest => dest.InterestType, opt => opt.MapFrom(src => src.InterestType))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.RepaymentFrequency, opt => opt.MapFrom(src => src.RepaymentFrequency))
                .ForMember(dest => dest.AllowOverpayment, opt => opt.MapFrom(src => src.AllowOverpayment))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
                .ForMember(dest => dest.Principal, opt => opt.Ignore())
                .ForMember(dest => dest.StartDate, opt => opt.Ignore());

            // Repayment mappings
            CreateMap<CreateRepaymentRequestDto, Repayment>()
                .ForMember(dest => dest.RepaymentDate, opt => opt.MapFrom(src => src.RepaymentDate))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
                .ForMember(dest => dest.Loan, opt => opt.Ignore());

            CreateMap<UpdateRepaymentRequestDto, Repayment>()
                .ForMember(dest => dest.RepaymentDate, opt => opt.MapFrom(src => src.RepaymentDate))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
                .ForMember(dest => dest.Loan, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.LoanId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<Repayment, RepaymentDto>();
            CreateMap<Repayment, RepaymentResponseDto>();
        }
    }
}