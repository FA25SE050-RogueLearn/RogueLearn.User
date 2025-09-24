using AutoMapper;
using RogueLearn.User.Application.Features.Products.Queries.GetProductById;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency));
    }
}