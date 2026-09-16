using FluentValidation;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand : IRequest<TokenDto>
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(v => v.AccessToken).NotEmpty().WithMessage("AccessToken is required.");
        RuleFor(v => v.RefreshToken).NotEmpty().WithMessage("RefreshToken is required.");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            throw new BadRequestException("Access token không hợp lệ.");
        }

        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (storedToken == null)
        {
            throw new BadRequestException("Refresh token không tồn tại.");
        }

        if (storedToken.ExpiryDate < DateTime.UtcNow)
        {
            throw new BadRequestException("Refresh token đã hết hạn.");
        }

        if (storedToken.IsRevoked)
        {
            throw new BadRequestException("Refresh token đã bị thu hồi.");
        }

        if (storedToken.IsUsed)
        {
            throw new BadRequestException("Refresh token đã qua sử dụng.");
        }

        storedToken.IsUsed = true;
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        var user = await _userRepository.GetUserWithRolesAndPermissionsAsync(storedToken.UserId, cancellationToken)
            ?? throw new NotFoundException("User", storedToken.UserId);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = (await _permissionRepository.GetPermissionsByUserIdAsync(user.Id, cancellationToken))
            .Select(p => p.Code).Distinct().ToList();

        var authResult = await _jwtTokenService.GenerateTokensAsync(user, roles, permissions, cancellationToken);

        return new TokenDto
        {
            AccessToken = authResult.AccessToken,
            RefreshToken = authResult.RefreshToken,
            TokenType = authResult.TokenType,
            ExpiresIn = authResult.ExpiresIn
        };
    }
}
