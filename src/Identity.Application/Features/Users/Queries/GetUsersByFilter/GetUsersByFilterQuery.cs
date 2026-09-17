using AutoMapper;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Users.Queries.GetUsersByFilter;

public record GetUsersByFilterQuery : IRequest<IReadOnlyList<UserDto>>
{
    public string? SearchTerm { get; init; }
    public Domain.Enums.UserStatus? Status { get; init; }
}

public class GetUsersByFilterQueryHandler : IRequestHandler<GetUsersByFilterQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUsersByFilterQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersByFilterQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            users = users.Where(u => u.UserName.ToLower().Contains(term) || u.Email.ToLower().Contains(term) || (u.FullName != null && u.FullName.ToLower().Contains(term))).ToList();
        }

        if (request.Status.HasValue)
        {
            users = users.Where(u => u.Status == request.Status.Value).ToList();
        }

        return _mapper.Map<IReadOnlyList<UserDto>>(users);
    }
}
