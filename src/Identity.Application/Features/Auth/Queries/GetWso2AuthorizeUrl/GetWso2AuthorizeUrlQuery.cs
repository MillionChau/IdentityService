using Identity.Application.Common.Interfaces;
using MediatR;

namespace Identity.Application.Features.Auth.Queries.GetWso2AuthorizeUrl;

public record GetWso2AuthorizeUrlQuery : IRequest<string>
{
    public string? State { get; init; }
}

public class GetWso2AuthorizeUrlQueryHandler : IRequestHandler<GetWso2AuthorizeUrlQuery, string>
{
    private readonly IWso2Service _wso2Service;

    public GetWso2AuthorizeUrlQueryHandler(IWso2Service wso2Service)
    {
        _wso2Service = wso2Service;
    }

    public Task<string> Handle(GetWso2AuthorizeUrlQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_wso2Service.GetAuthorizeUrl(request.State));
    }
}
