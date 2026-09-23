using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.Auth.Commands.ChangePassword;
using Identity.Application.Features.Auth.Commands.Login;
using Identity.Application.Features.Auth.Commands.RefreshToken;
using Identity.Application.Features.Auth.Commands.Register;
using Identity.Application.Features.Auth.Commands.RevokeToken;
using Identity.Application.Features.Auth.Commands.OAuthLogin;
using Identity.Application.Features.Auth.Queries.GetCurrentUser;
using Identity.Application.Features.Auth.Queries.GetOAuthAuthorizeUrl;
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
    // OAuth2 Endpoints (GitHub, Google) cho DevRadar
    // ==========================================

    /// <summary>
    /// Lấy URL chuyển hướng người dùng đến trang đăng nhập của nhà cung cấp OAuth2 (vd: github, google)
    /// </summary>
    [HttpGet("oauth/{provider}/authorize-url")]
    [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOAuthAuthorizeUrl([FromRoute] string provider, [FromQuery] string? state, CancellationToken cancellationToken)
    {
        var url = await Mediator.Send(new GetOAuthAuthorizeUrlQuery { Provider = provider, State = state }, cancellationToken);
        return Ok(ResponseModel<string>.Success(url));
    }

    /// <summary>
    /// Callback từ nhà cung cấp OAuth2 sau khi người dùng chấp thuận cấp quyền
    /// </summary>
    [HttpGet("oauth/{provider}/callback")]
    [ProducesResponseType(typeof(ResponseModel<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OAuthCallback([FromRoute] string provider, [FromQuery] string code, [FromQuery] string? state, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new OAuthLoginCommand
        {
            Provider = provider,
            Code = code
        }, cancellationToken);

        return Ok(ResponseModel<AuthResponseDto>.Success(response, $"Xác thực {provider} OAuth2 thành công."));
    }

    /// <summary>
    /// API tiếp nhận mã Authorization Code từ Frontend SPA / Mobile Client để hoàn tất đăng nhập OAuth2
    /// </summary>
    [HttpPost("oauth/{provider}/login")]
    [ProducesResponseType(typeof(ResponseModel<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OAuthLogin([FromRoute] string provider, [FromBody] OAuthLoginCommand command, CancellationToken cancellationToken)
    {
        var commandWithProvider = command with { Provider = provider };
        var response = await Mediator.Send(commandWithProvider, cancellationToken);
        return Ok(ResponseModel<AuthResponseDto>.Success(response, $"Đăng nhập {provider} OAuth2 thành công."));
    }
}
