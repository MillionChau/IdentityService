using AutoMapper;
using Identity.Application.Common.Exceptions;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserDto>
{
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
}

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetCurrentUserQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        Domain.Entities.User? user = null;

        if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
        {
            user = await _userRepository.GetUserWithRolesAndPermissionsAsync(request.UserId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var existing = await _userRepository.GetByUserNameAsync(request.UserName.Trim(), cancellationToken);
            if (existing != null)
            {
                user = await _userRepository.GetUserWithRolesAndPermissionsAsync(existing.Id, cancellationToken);
            }
        }

        if (user == null)
        {
            throw new NotFoundException("Không tìm thấy thông tin người dùng.");
        }

        return _mapper.Map<UserDto>(user);
    }
}
