using FluentValidation;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.OAuthLogin;

public record OAuthLoginCommand : IRequest<AuthResponseDto>
{
    public string Provider { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? RedirectUri { get; init; }
}

public class OAuthLoginCommandValidator : AbstractValidator<OAuthLoginCommand>
{
    public OAuthLoginCommandValidator()
    {
        RuleFor(v => v.Provider)
            .NotEmpty().WithMessage("Provider is required.");

        RuleFor(v => v.Code)
            .NotEmpty().WithMessage("Authorization code is required.");
    }
}

public class OAuthLoginCommandHandler : IRequestHandler<OAuthLoginCommand, AuthResponseDto>
{
    private readonly IOAuthService _oauthService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OAuthLoginCommandHandler(
        IOAuthService oauthService,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _oauthService = oauthService;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(OAuthLoginCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AuthProvider>(request.Provider, true, out var provider) || provider == AuthProvider.Local)
        {
            throw new BadRequestException($"Nhà cung cấp OAuth '{request.Provider}' không hợp lệ hoặc không được hỗ trợ.");
        }

        var userInfo = await _oauthService.AuthenticateCodeAsync(provider, request.Code, request.RedirectUri, cancellationToken);
        if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.ProviderKey))
        {
            throw new BadRequestException("Không thể lấy thông tin định danh từ nhà cung cấp OAuth.");
        }

        // 1. Tìm theo liên kết ExternalLogin đã có
        var user = await _userRepository.GetByExternalLoginAsync(provider, userInfo.ProviderKey, cancellationToken);

        // 2. Nếu chưa liên kết theo ProviderKey, tìm theo Email
        if (user == null && !string.IsNullOrWhiteSpace(userInfo.Email))
        {
            user = await _userRepository.GetByEmailAsync(userInfo.Email.Trim().ToLowerInvariant(), cancellationToken);
            if (user != null)
            {
                // Liên kết tài khoản hiện có với provider
                user.ExternalLogins.Add(new UserExternalLogin
                {
                    UserId = user.Id,
                    Provider = provider,
                    ProviderKey = userInfo.ProviderKey,
                    ProviderDisplayName = provider.ToString()
                });

                if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(userInfo.AvatarUrl))
                {
                    user.AvatarUrl = userInfo.AvatarUrl;
                }

                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        // 3. Nếu vẫn chưa có tài khoản, tự động tạo mới
        if (user == null)
        {
            var baseUsername = !string.IsNullOrWhiteSpace(userInfo.Username) ? userInfo.Username.Trim() : $"{provider.ToString().ToLower()}_{userInfo.ProviderKey}";
            var username = baseUsername;
            var counter = 1;

            while (await _userRepository.GetByUserNameAsync(username, cancellationToken) != null)
            {
                username = $"{baseUsername}_{counter++}";
            }

            var email = !string.IsNullOrWhiteSpace(userInfo.Email) 
                ? userInfo.Email.Trim().ToLowerInvariant() 
                : $"{username}@{provider.ToString().ToLower()}.oauth.devradar";

            var fullName = userInfo.FullName;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = string.Join(" ", new[] { userInfo.LastName, userInfo.FirstName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            if (string.IsNullOrWhiteSpace(fullName)) fullName = username;

            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = username,
                Email = email,
                FullName = fullName,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                AvatarUrl = userInfo.AvatarUrl,
                Status = UserStatus.Active,
                PrimaryProvider = provider,
                ExternalId = userInfo.ProviderKey
            };

            user.ExternalLogins.Add(new UserExternalLogin
            {
                UserId = user.Id,
                Provider = provider,
                ProviderKey = userInfo.ProviderKey,
                ProviderDisplayName = provider.ToString()
            });

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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

        var roles = user.UserRoles?.Where(ur => ur.Role != null).Select(ur => ur.Role.Name).ToList() ?? new List<string>();
        if (!roles.Any()) roles.Add("User");

        var userPermissions = await _permissionRepository.GetPermissionsByUserIdAsync(user.Id, cancellationToken);
        var permissions = userPermissions != null
            ? userPermissions.Where(p => p != null).Select(p => p.Code).Distinct().ToList()
            : new List<string>();

        return await _jwtTokenService.GenerateTokensAsync(user, roles, permissions, cancellationToken);
    }
}

