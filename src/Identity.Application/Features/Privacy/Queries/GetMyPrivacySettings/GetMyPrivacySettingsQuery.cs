using AutoMapper;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Features.Privacy.Queries.GetMyPrivacySettings;

public record GetMyPrivacySettingsQuery : IRequest<UserPrivacySettingDto>;

public class GetMyPrivacySettingsQueryHandler : IRequestHandler<GetMyPrivacySettingsQuery, UserPrivacySettingDto>
{
    private readonly IUserPrivacyRepository _privacyRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyPrivacySettingsQueryHandler(
        IUserPrivacyRepository privacyRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _privacyRepository = privacyRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserPrivacySettingDto> Handle(GetMyPrivacySettingsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xem cài đặt quyền riêng tư.");
        }

        var userId = _currentUserService.UserId.Value;
        var setting = await _privacyRepository.GetByUserIdAsync(userId, cancellationToken);

        if (setting == null)
        {
            // Tự động tạo bản ghi mặc định an toàn nếu chưa có
            setting = new UserPrivacySetting
            {
                UserId = userId,
                IsProfilePublic = true,
                ShowEmail = false,
                ShowPhoneNumber = false,
                ShowGitHubStats = true,
                ShowActivityStats = true
            };

            await _privacyRepository.AddAsync(setting, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return _mapper.Map<UserPrivacySettingDto>(setting);
    }
}
