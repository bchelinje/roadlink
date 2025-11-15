using AutoMapper;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.Users.ViewModels;

namespace BeC.OpenId.Connect.Features.Users.MappingProfiles;

/// <summary>
/// AutoMapper profile for User entities and view models
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<ApplicationUser, UserViewModel>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.Claims, opt => opt.Ignore());
    }
}
