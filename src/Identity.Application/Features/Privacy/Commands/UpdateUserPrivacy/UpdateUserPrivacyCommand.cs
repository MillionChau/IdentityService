using AutoMapper;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Features.Privacy.Commands.UpdateUserPrivacy;

public record UpdateUserPrivacyCommand : IRequest<UserPrivacySettingDto>
{
    public bool IsProfilePublic { get; init; } = true;
    public bool ShowEmail { get; init; } = false;
    public bool ShowPhoneNumber { get; init; } = false;
    public bool ShowGitHubStats { get; init; } = true;
    public bool ShowActivityStats { get; init; } = true;
}

public class UpdateUserPrivacyCommandHandler : IRequestHandler<UpdateUserPrivacyCommand, UserPrivacySettingDto>
{
    private readonly IUserPrivacyRepository _privacyRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateUserPrivacyCommandHandler(
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

    public async Task<UserPrivacySettingDto> Handle(UpdateUserPrivacyCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để cập nhật cài đặt quyền riêng tư.");
        }

        var userId = _currentUserService.UserId.Value;
        var setting = await _privacyRepository.GetByUserIdAsync(userId, cancellationToken);

        if (setting == null)
        {
            setting = new UserPrivacySetting
            {
                UserId = userId,
                IsProfilePublic = request.IsProfilePublic,
                ShowEmail = request.ShowEmail,
                ShowPhoneNumber = request.ShowPhoneNumber,
                ShowGitHubStats = request.ShowGitHubStats,
                ShowActivityStats = request.ShowActivityStats
            };

            await _privacyRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.IsProfilePublic = request.IsProfilePublic;
            setting.ShowEmail = request.ShowEmail;
            setting.ShowPhoneNumber = request.ShowPhoneNumber;
            setting.ShowGitHubStats = request.ShowGitHubStats;
            setting.ShowActivityStats = request.ShowActivityStats;

            await _privacyRepository.UpdateAsync(setting, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserPrivacySettingDto>(setting);
    }
}
