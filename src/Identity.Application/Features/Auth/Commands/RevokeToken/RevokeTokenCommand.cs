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

    public RevokeTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var stored = await _refreshTokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (stored != null)
        {
            stored.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
