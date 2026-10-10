using AutoMapper;
using ProductsApi.DTOs;
using ProductsApi.Models;

namespace ProductsApi.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Product, ProductDto>().ReverseMap()
            .ForMember(product => product.Category, options => options.Ignore());

        CreateMap<ProductRequestDto, Product>()
            .ForMember(product => product.Id, options => options.Ignore())
            .ForMember(product => product.Name, options => options.MapFrom(dto => dto.Name.Trim()))
            .ForMember(product => product.Category, options => options.MapFrom(dto => dto.Category.Trim()));
    }
}
