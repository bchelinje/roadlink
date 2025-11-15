using AutoMapper;
using BeC.OpenId.Connect.Features.Location.Dtos;
using BeC.OpenId.Connect.Features.Location.ViewModels;

namespace BeC.OpenId.Connect.Features.Location.MappingProfiles;

/// <summary>
/// AutoMapper profile for Location mappings
/// </summary>
public class LocationMappingProfile : Profile
{
    public LocationMappingProfile()
    {
        // DriverLocation entity to LocationViewModel
        CreateMap<DriverLocation, LocationViewModel>();
    }
}
