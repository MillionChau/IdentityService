using FluentValidation;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.Login;

public record LoginCommand : IRequest<AuthResponseDto>
{
    public string UserNameOrEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(v => v.UserNameOrEmail)
            .NotEmpty().WithMessage("Username or Email is required.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPermissionRepository _permissionRepository;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IPermissionRepository permissionRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _permissionRepository = permissionRepository;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        User? user;
        if (request.UserNameOrEmail.Contains('@'))
        {
            user = await _userRepository.GetByEmailAsync(request.UserNameOrEmail.Trim().ToLowerInvariant(), cancellationToken);
        }
        else
        {
            user = await _userRepository.GetByUserNameAsync(request.UserNameOrEmail.Trim(), cancellationToken);
        }

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BadRequestException("Tên đăng nhập/email hoặc mật khẩu không chính xác.");
        }

        if (user.Status == UserStatus.Inactive)
        {
            throw new BadRequestException("Tài khoản chưa được kích hoạt.");
        }

        if (user.Status == UserStatus.Locked)
        {
            throw new BadRequestException("Tài khoản đã bị khoá.");
        }

        user = await _userRepository.GetUserWithRolesAndPermissionsAsync(user.Id, cancellationToken) ?? user;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = (await _permissionRepository.GetPermissionsByUserIdAsync(user.Id, cancellationToken))
            .Select(p => p.Code).Distinct().ToList();

        return await _jwtTokenService.GenerateTokensAsync(user, roles, permissions, cancellationToken);
    }
}
