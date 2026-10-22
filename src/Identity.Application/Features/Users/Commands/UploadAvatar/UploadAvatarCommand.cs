using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Users.Commands.UploadAvatar;

public record UploadAvatarCommand : IRequest<string>
{
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
}

public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, string>
{
    private readonly IStorageService _storageService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UploadAvatarCommandHandler(
        IStorageService storageService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _storageService = storageService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedException("Authentication is required.");

        var userId = _currentUser.UserId.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        // Upload và nén ảnh đại diện 256x256 sang MinIO (Bucket: devradar-avatars)
        var avatarUrl = await _storageService.UploadImageAsync(
            request.FileStream,
            request.FileName,
            StorageBuckets.Avatars,
            maxWidth: 256,
            maxHeight: 256,
            cancellationToken);

        user.AvatarUrl = avatarUrl;
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return avatarUrl;
    }
}
