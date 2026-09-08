using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.Auth.Commands.ChangePassword;
using Identity.Application.Features.Auth.Commands.Login;
using Identity.Application.Features.Auth.Commands.RefreshToken;
using Identity.Application.Features.Auth.Commands.Register;
using Identity.Application.Features.Auth.Commands.RevokeToken;
using Identity.Application.Features.Auth.Commands.Wso2ExchangeToken;
using Identity.Application.Features.Auth.Commands.Wso2RenewToken;
using Identity.Application.Features.Auth.Queries.GetCurrentUser;
using Identity.Application.Features.Auth.Queries.GetWso2AuthorizeUrl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public AuthController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Đăng nhập tài khoản trực tiếp (Độc lập, không cần mạng nội bộ)
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ResponseModel<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ResponseModel<AuthResponseDto>.Success(result, "Đăng nhập thành công."));
    }

    /// <summary>
    /// Đăng ký tài khoản người dùng mới (Độc lập, không cần mạng nội bộ)
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ResponseModel<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ResponseModel<AuthResponseDto>.Success(result, "Đăng ký tài khoản thành công."));
    }

    /// <summary>
    /// Gia hạn access token bằng refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ResponseModel<TokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ResponseModel<TokenDto>.Success(result, "Gia hạn token thành công."));
    }

    /// <summary>
    /// Đăng xuất / Thu hồi phiên đăng nhập
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ResponseModel<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RevokeTokenCommand? command, CancellationToken cancellationToken)
    {
        var token = command?.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            var authHeader = Request.Headers.Authorization.ToString();
            token = authHeader.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        var result = await Mediator.Send(new RevokeTokenCommand { Token = token ?? string.Empty }, cancellationToken);
        return Ok(ResponseModel<bool>.Success(result, "Đăng xuất thành công."));
    }

    /// <summary>
    /// Lấy thông tin tài khoản người dùng đang đăng nhập
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ResponseModel<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCurrentUserQuery
        {
            UserId = _currentUserService.UserId,
            UserName = _currentUserService.UserName
        }, cancellationToken);

        return Ok(ResponseModel<UserDto>.Success(result));
    }

    /// <summary>
    /// Đổi mật khẩu tài khoản hiện tại
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ResponseModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(ResponseModel<object>.Failure("Chưa xác thực danh tính."));
        }

        var command = new ChangePasswordCommand
        {
            UserId = _currentUserService.UserId.Value,
            CurrentPassword = dto.CurrentPassword,
            NewPassword = dto.NewPassword
        };

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ResponseModel<bool>.Success(result, "Đổi mật khẩu thành công."));
    }

    // ==========================================
    // WSO2 OAuth2 / OpenID Connect Endpoints (Kế thừa từ IdentityService csdl-hn-net)
    // ==========================================

    /// <summary>
    /// Lấy URL đăng nhập qua máy chủ WSO2 Identity Server
    /// </summary>
    [HttpGet("wso2/authorize-url")]
    [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWso2AuthorizeUrl([FromQuery] string? state, CancellationToken cancellationToken)
    {
        var url = await Mediator.Send(new GetWso2AuthorizeUrlQuery { State = state }, cancellationToken);
        return Ok(ResponseModel<string>.Success(url));
    }

    /// <summary>
    /// API lấy token từ hệ thống WSO2 sau khi có Authorization Code (Tương thích endpoint /connection/token cũ)
    /// </summary>
    [HttpGet("connection/token")]
    [ProducesResponseType(typeof(ResponseModel<Wso2TokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConnectWso2Token([FromQuery] string code, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new Wso2ExchangeTokenCommand { Code = code }, cancellationToken);
        return Ok(ResponseModel<Wso2TokenDto>.Success(response, "Đổi mã WSO2 thành công."));
    }

    /// <summary>
    /// Callback nhận authorization code từ WSO2
    /// </summary>
    [HttpGet("wso2/callback")]
    [ProducesResponseType(typeof(ResponseModel<Wso2TokenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Wso2Callback([FromQuery] string code, [FromQuery] string? state, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new Wso2ExchangeTokenCommand { Code = code }, cancellationToken);
        return Ok(ResponseModel<Wso2TokenDto>.Success(response, "Xác thực WSO2 thành công."));
    }

    /// <summary>
    /// Gia hạn phiên đăng nhập WSO2 mà không bắt người dùng đăng nhập lại (Tương thích /connection/token/renew)
    /// </summary>
    [HttpPost("connection/token/renew")]
    [ProducesResponseType(typeof(ResponseModel<Wso2TokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenewWso2Token([FromForm] string? accessToken, [FromForm] string? refreshToken, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new Wso2RenewTokenCommand
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }, cancellationToken);

        return Ok(ResponseModel<Wso2TokenDto>.Success(response, "Gia hạn phiên WSO2 thành công."));
    }
}
