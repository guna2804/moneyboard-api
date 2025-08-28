using MoneyBoard.Application.DTOs;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Enums;

namespace MoneyBoard.Application.Mappings
{
    public class AuthMappingProfile : BaseMappingProfile
    {
        protected override void CreateMaps()
        {
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(_ => RolesType.User))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Will be set by service

            CreateMap<User, AuthResponseDto>()
                .ConstructUsing((user, context) =>
                    new AuthResponseDto(
                        user.Email,
                        user.FullName,
                        context.Items["Token"] as string ?? string.Empty,
                        context.Items["RefreshToken"] as string ?? string.Empty));
        }
    }
}
