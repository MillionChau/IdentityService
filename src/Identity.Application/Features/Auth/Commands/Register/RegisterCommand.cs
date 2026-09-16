using FluentValidation;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.Register;

public record RegisterCommand : IRequest<AuthResponseDto>
{
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(v => v.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScimService _scimService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        IScimService scimService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _scimService = scimService;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var exists = await _userRepository.ExistsByUserNameOrEmailAsync(request.UserName.Trim(), request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (exists)
        {
            throw new BadRequestException("Tên đăng nhập hoặc Email đã tồn tại trên hệ thống.");
        }

        var fullName = string.Join(" ", new[] { request.LastName, request.FirstName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(fullName)) fullName = request.UserName;

        var user = new User
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            FullName = fullName,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Address = request.Address?.Trim(),
            Status = UserStatus.Active,
            PrimaryProvider = AuthProvider.Local
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _scimService.CreateUserAsync(user, request.Password, cancellationToken);
        }
        catch
        {
            // Graceful non-blocking WSO2 sync
        }

        var roles = new List<string> { "User" };
        var permissions = new List<string>();

        return await _jwtTokenService.GenerateTokensAsync(user, roles, permissions, cancellationToken);
    }
}
