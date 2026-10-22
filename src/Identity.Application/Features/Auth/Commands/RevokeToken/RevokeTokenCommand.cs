using Identity.Application.Common.Interfaces;
using Identity.Domain.Contracts;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.RevokeToken;

public record RevokeTokenCommand : IRequest<bool>
{
    public string Token { get; init; } = string.Empty;
}

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCacheService _redisCacheService;

    public RevokeTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IRedisCacheService redisCacheService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _redisCacheService = redisCacheService;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return true;

        var stored = await _refreshTokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (stored != null)
        {
            stored.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _redisCacheService.RemoveAsync($"refresh_token:{request.Token}", cancellationToken);
        }

        // Add token (both access or refresh) to Redis blacklist with 7 days TTL (standard token expiration)
        await _redisCacheService.BlacklistTokenAsync(request.Token, TimeSpan.FromDays(7), cancellationToken);

        return true;
    }
}
