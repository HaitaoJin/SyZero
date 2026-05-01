using AutoMapper;
using SyZero.Example3.Core.Entities;
using SyZero.Example3.IApplication.Examples.Dto;

namespace SyZero.Example3.Application.MapProfile
{
    public class AppMapProfile : Profile
    {
        public AppMapProfile()
        {
            CreateMap<Example, CreateExample3Dto>();
            CreateMap<CreateExample3Dto, Example>();
            CreateMap<Example, Example3Dto>();
            CreateMap<Example3Dto, Example>();
        }
    }
}
