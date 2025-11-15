using System.Text.Json;
using AutoMapper;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Vehicles.ViewModels;

namespace BeC.OpenId.Connect.Features.Vehicles.MappingProfiles;

/// <summary>
/// AutoMapper profile for Vehicle mappings
/// </summary>
public class VehicleMappingProfile : Profile
{
    public VehicleMappingProfile()
    {
        // Vehicle entity to VehicleViewModel
        CreateMap<Vehicle, VehicleViewModel>()
            .ForMember(dest => dest.Features, opt => opt.MapFrom(src =>
                !string.IsNullOrWhiteSpace(src.Features)
                    ? JsonSerializer.Deserialize<List<string>>(src.Features)
                    : null))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src =>
                !string.IsNullOrWhiteSpace(src.Photos)
                    ? JsonSerializer.Deserialize<List<string>>(src.Photos)
                    : null))
            .ForMember(dest => dest.Driver, opt => opt.MapFrom(src => src.Driver));

        // Driver entity to DriverInfo (nested in VehicleViewModel)
        CreateMap<Driver, DriverInfo>();

        // ActivityLog to MaintenanceHistoryViewModel
        CreateMap<ActivityLog, MaintenanceHistoryViewModel>()
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName ?? string.Empty));
    }
}
