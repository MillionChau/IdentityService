using AutoMapper;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand : IRequest<UserDto>
{
    public Guid Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Address { get; init; }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScimService _scimService;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IScimService scimService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _scimService = scimService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserWithRolesAndPermissionsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        user.FirstName = request.FirstName?.Trim() ?? user.FirstName;
        user.LastName = request.LastName?.Trim() ?? user.LastName;
        user.PhoneNumber = request.PhoneNumber?.Trim() ?? user.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl?.Trim() ?? user.AvatarUrl;
        user.Address = request.Address?.Trim() ?? user.Address;

        var fullName = string.Join(" ", new[] { user.LastName, user.FirstName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            user.FullName = fullName;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _scimService.UpdateUserAsync(user, cancellationToken);
        }
        catch
        {
            // Non-blocking SCIM sync
        }

        return _mapper.Map<UserDto>(user);
    }
}
