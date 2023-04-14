using AutoMapper;
using Tms.Adapter.Models;
using TmsRunner.Models;

namespace TmsRunner.Mapper;

public static class MapperFactory
{
    // TODO Remove automapper
    public static IMapper ConfigureMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Step, Step>()
                .ForMember(dest => dest.Steps, opt => opt.Ignore());
            cfg.CreateMap<StepResult, Step>();
        });

        return config.CreateMapper();
    }
}