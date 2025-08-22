using AutoMapper;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Application.DTOs;

namespace MoneyBoard.Application.Mappings
{
    public abstract class BaseMappingProfile : Profile
    {
        protected BaseMappingProfile()
        {
            // Common configuration that applies to all mappings
            CreateMaps();
        }

        protected abstract void CreateMaps();
    }
}