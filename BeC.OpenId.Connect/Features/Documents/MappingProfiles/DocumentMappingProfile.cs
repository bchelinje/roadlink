using AutoMapper;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Documents.ViewModels;
using BeC.OpenId.Connect.Features.Drivers.Dtos;

namespace BeC.OpenId.Connect.Features.Documents.MappingProfiles;

/// <summary>
/// AutoMapper profile for Document mappings
/// </summary>
public class DocumentMappingProfile : Profile
{
    public DocumentMappingProfile()
    {
        // DriverDocument entity to DocumentViewModel
        CreateMap<DriverDocument, DocumentViewModel>()
            .ForMember(dest => dest.Driver, opt => opt.MapFrom(src => src.Driver));

        // Driver entity to DocumentDriverInfo (nested in DocumentViewModel)
        CreateMap<Driver, DocumentDriverInfo>();
    }
}
