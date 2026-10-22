using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Users.Commands.UploadCover;

public record UploadCoverCommand : IRequest<string>
{
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
}

public class UploadCoverCommandHandler : IRequestHandler<UploadCoverCommand, string>
{
    private readonly IStorageService _storageService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UploadCoverCommandHandler(
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

    public async Task<string> Handle(UploadCoverCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedException("Authentication is required.");

        // Upload và nén ảnh bìa 1200x400 sang MinIO (Bucket: devradar-covers)
        var coverUrl = await _storageService.UploadImageAsync(
            request.FileStream,
            request.FileName,
            StorageBuckets.Covers,
            maxWidth: 1200,
            maxHeight: 400,
            cancellationToken);

        return coverUrl;
    }
}
