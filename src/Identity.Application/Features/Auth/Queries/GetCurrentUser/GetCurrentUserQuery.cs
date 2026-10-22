using AutoMapper;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
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
    private readonly IRedisCacheService _redisCacheService;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        IRedisCacheService redisCacheService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _redisCacheService = redisCacheService;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        // Try getting from Redis cache if UserId is provided
        if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
        {
            var cachedUser = await _redisCacheService.GetAsync<UserDto>($"user:profile:{request.UserId.Value}", cancellationToken);
            if (cachedUser != null)
            {
                return cachedUser;
            }
        }

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

        var dto = _mapper.Map<UserDto>(user);

        // Cache for 30 minutes
        await _redisCacheService.SetAsync($"user:profile:{user.Id}", dto, TimeSpan.FromMinutes(30), cancellationToken);

        return dto;
    }
}
