using AutoMapper;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Menus.Queries.GetUserMenus;

public record GetUserMenusQuery(Guid UserId) : IRequest<IReadOnlyList<MenuDto>>;

public class GetUserMenusQueryHandler : IRequestHandler<GetUserMenusQuery, IReadOnlyList<MenuDto>>
{
    private readonly IMenuRepository _menuRepository;
    private readonly IMapper _mapper;

    public GetUserMenusQueryHandler(IMenuRepository menuRepository, IMapper mapper)
    {
        _menuRepository = menuRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<MenuDto>> Handle(GetUserMenusQuery request, CancellationToken cancellationToken)
    {
        var menus = await _menuRepository.GetMenusForUserAsync(request.UserId, cancellationToken);
        return _mapper.Map<IReadOnlyList<MenuDto>>(menus);
    }
}
