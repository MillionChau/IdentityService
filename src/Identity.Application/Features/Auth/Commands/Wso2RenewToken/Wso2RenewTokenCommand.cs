using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.Wso2RenewToken;

public record Wso2RenewTokenCommand : IRequest<Wso2TokenDto>
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
}

public class Wso2RenewTokenCommandHandler : IRequestHandler<Wso2RenewTokenCommand, Wso2TokenDto>
{
    private readonly IWso2Service _wso2Service;

    public Wso2RenewTokenCommandHandler(IWso2Service wso2Service)
    {
        _wso2Service = wso2Service;
    }

    public async Task<Wso2TokenDto> Handle(Wso2RenewTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = request.RefreshToken ?? request.AccessToken;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new BadRequestException("Thiếu refresh token để gia hạn phiên.");
        }

        return await _wso2Service.RefreshTokenAsync(refreshToken, cancellationToken);
    }
}
