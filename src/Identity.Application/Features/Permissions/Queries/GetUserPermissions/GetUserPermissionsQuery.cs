using AutoMapper;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Permissions.Queries.GetUserPermissions;

public record GetUserPermissionsQuery(Guid UserId) : IRequest<IReadOnlyList<PermissionDto>>;

public class GetUserPermissionsQueryHandler : IRequestHandler<GetUserPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;

    public GetUserPermissionsQueryHandler(IPermissionRepository permissionRepository, IMapper mapper)
    {
        _permissionRepository = permissionRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PermissionDto>> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(request.UserId, cancellationToken);
        return _mapper.Map<IReadOnlyList<PermissionDto>>(permissions);
    }
}
