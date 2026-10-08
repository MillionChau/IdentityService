using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Features.Auth.Queries.GetOAuthAuthorizeUrl;

public record GetOAuthAuthorizeUrlQuery : IRequest<string>
{
    public string Provider { get; init; } = string.Empty;
    public string? State { get; init; }
}

public class GetOAuthAuthorizeUrlQueryHandler : IRequestHandler<GetOAuthAuthorizeUrlQuery, string>
{
    private readonly IOAuthService _oauthService;

    public GetOAuthAuthorizeUrlQueryHandler(IOAuthService oauthService)
    {
        _oauthService = oauthService;
    }

    public Task<string> Handle(GetOAuthAuthorizeUrlQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AuthProvider>(request.Provider, true, out var provider) || provider == AuthProvider.Local)
        {
            throw new BadRequestException($"Nhà cung cấp OAuth '{request.Provider}' không hợp lệ hoặc không được hỗ trợ.");
        }

        var url = _oauthService.GetAuthorizationUrl(provider, request.State);
        return Task.FromResult(url);
    }
}

