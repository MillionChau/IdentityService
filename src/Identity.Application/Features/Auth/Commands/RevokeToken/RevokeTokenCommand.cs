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
    private readonly IWso2Service _wso2Service;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IWso2Service wso2Service,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _wso2Service = wso2Service;
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

        try
        {
            await _wso2Service.RevokeTokenAsync(request.Token, cancellationToken);
        }
        catch
        {
            // Ignore WSO2 revoke error if unreachable
        }

        return true;
    }
}
