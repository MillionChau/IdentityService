using AutoMapper;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Users.Queries.GetUserPublicProfile;

public record GetUserPublicProfileQuery(Guid Id) : IRequest<PublicUserProfileDto>;

/// <summary>
/// Truy vấn xem hồ sơ thành viên khác (UC-07) tích hợp Privacy Filter Engine tự động.
/// </summary>
public class GetUserPublicProfileQueryHandler : IRequestHandler<GetUserPublicProfileQuery, PublicUserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetUserPublicProfileQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PublicUserProfileDto> Handle(GetUserPublicProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserWithRolesAndPermissionsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        var dto = _mapper.Map<PublicUserProfileDto>(user);

        // Kiểm tra xem người gọi có phải là chính chủ sở hữu hoặc Admin không
        var isOwner = _currentUserService.IsAuthenticated && _currentUserService.UserId == request.Id;
        var isAdmin = _currentUserService.IsAuthenticated && _currentUserService.Roles.Contains("Admin");

        if (isOwner || isAdmin)
        {
            // Chính chủ hoặc Admin: hiển thị đầy đủ thông tin
            return dto;
        }

        // Người xem là Guest hoặc Member khác: áp dụng Privacy Filter
        var privacy = user.PrivacySetting;
        var isPublic = privacy?.IsProfilePublic ?? true;

        if (!isPublic)
        {
            // Hồ sơ ở chế độ riêng tư
            dto.IsProfilePublic = false;
            dto.Email = null;
            dto.PhoneNumber = null;
            dto.Address = null;
            dto.ShowGitHubStats = false;
            dto.ShowActivityStats = false;
            return dto;
        }

        // Hồ sơ công khai: áp dụng bộ lọc từng trường dữ liệu
        var showEmail = privacy?.ShowEmail ?? false;
        var showPhone = privacy?.ShowPhoneNumber ?? false;
        var showGitHub = privacy?.ShowGitHubStats ?? true;
        var showActivity = privacy?.ShowActivityStats ?? true;

        dto.Email = showEmail ? user.Email : null;
        dto.PhoneNumber = showPhone ? user.PhoneNumber : null;
        dto.Address = null; // Luôn ẩn địa chỉ chi tiết với người ngoài
        dto.ShowGitHubStats = showGitHub;
        dto.ShowActivityStats = showActivity;

        return dto;
    }
}
