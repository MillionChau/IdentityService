using AutoMapper;
using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Roles, opt => opt.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name)))
            .ForMember(d => d.Permissions, opt => opt.MapFrom(s => s.UserRoles.SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code)).Distinct()));

        CreateMap<Menu, MenuDto>()
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.Children));

        CreateMap<Permission, PermissionDto>();
    }
}
