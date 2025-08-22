using AutoMapper;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Domain.Entities;

namespace MoneyBoard.Application.Mappings
{
    public class NotificationMappingProfile : BaseMappingProfile
    {
        protected override void CreateMaps()
        {
            CreateMap<Notification, NotificationDetailsDto>()
                .ReverseMap();

            CreateMap<NotificationDto, Notification>()
                .ForMember(dest => dest.Loan, opt => opt.Ignore())
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(_ => DateTime.UtcNow));
        }
    }
}