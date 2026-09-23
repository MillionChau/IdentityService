using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.Wso2ExchangeToken;

public record Wso2ExchangeTokenCommand : IRequest<Wso2TokenDto>
{
    public string Code { get; init; } = string.Empty;
}

public class Wso2ExchangeTokenCommandHandler : IRequestHandler<Wso2ExchangeTokenCommand, Wso2TokenDto>
{
    private readonly IWso2Service _wso2Service;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public Wso2ExchangeTokenCommandHandler(
        IWso2Service wso2Service,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _wso2Service = wso2Service;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Wso2TokenDto> Handle(Wso2ExchangeTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new BadRequestException("Authorization code không được để trống.");
        }

        var wso2Tokens = await _wso2Service.ExchangeCodeForTokenAsync(request.Code, cancellationToken);
        return wso2Tokens;
    }
}
