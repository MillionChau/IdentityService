using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.Privacy.Commands.UpdateUserPrivacy;
using Identity.Application.Features.Privacy.Queries.GetMyPrivacySettings;
using Identity.Application.Features.Users.Commands.DeleteUser;
using Identity.Application.Features.Users.Commands.LockUser;
using Identity.Application.Features.Users.Commands.UnlockUser;
using Identity.Application.Features.Users.Commands.UpdateUser;
using Identity.Application.Features.Users.Commands.UploadAvatar;
using Identity.Application.Features.Users.Commands.UploadCover;
using Identity.Application.Features.Users.Queries.GetUserById;
using Identity.Application.Features.Users.Queries.GetUsersByFilter;
using Identity.Application.Features.Users.Queries.GetUserPublicProfile;
using Identity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ApiControllerBase
{
    /// <summary>
    /// Lấy danh sách người dùng theo bộ lọc
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResponseModel<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] string? searchTerm, [FromQuery] UserStatus? status, CancellationToken cancellationToken)
    {
        var query = new GetUsersByFilterQuery
        {
            SearchTerm = searchTerm,
            Status = status
        };

        var users = await Mediator.Send(query, cancellationToken);
        return Ok(ResponseModel<IReadOnlyList<UserDto>>.Success(users));
    }

    /// <summary>
    /// Lấy thông tin chi tiết người dùng theo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResponseModel<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var user = await Mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(ResponseModel<UserDto>.Success(user));
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân người dùng
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ResponseModel<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand
        {
            Id = id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            AvatarUrl = dto.AvatarUrl,
            Address = dto.Address
        };

        var user = await Mediator.Send(command, cancellationToken);
        return Ok(ResponseModel<UserDto>.Success(user, "Cập nhật thông tin thành công."));
    }

    /// <summary>
    /// Xoá người dùng (Soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ResponseModel<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return Ok(ResponseModel<bool>.Success(result, "Xoá người dùng thành công."));
    }

    /// <summary>
    /// Khóa tài khoản người dùng vi phạm (Admin - UC-66)
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    [ProducesResponseType(typeof(ResponseModel<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LockUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new LockUserCommand(id), cancellationToken);
        return Ok(ResponseModel<bool>.Success(result, "Tài khoản đã được khóa thành công."));
    }

    /// <summary>
    /// Mở khóa tài khoản người dùng (Admin - UC-66)
    /// </summary>
    [HttpPost("{id:guid}/unlock")]
    [ProducesResponseType(typeof(ResponseModel<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UnlockUserCommand(id), cancellationToken);
        return Ok(ResponseModel<bool>.Success(result, "Tài khoản đã được mở khóa thành công."));
    }

    /// <summary>
    /// Lấy cài đặt quyền riêng tư của tài khoản đang đăng nhập (UC-09)
    /// </summary>
    [HttpGet("me/privacy")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseModel<UserPrivacySettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPrivacySettings(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMyPrivacySettingsQuery(), cancellationToken);
        return Ok(ResponseModel<UserPrivacySettingDto>.Success(result));
    }

    /// <summary>
    /// Cập nhật cài đặt quyền riêng tư của tài khoản đang đăng nhập (UC-09)
    /// </summary>
    [HttpPut("me/privacy")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseModel<UserPrivacySettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyPrivacySettings([FromBody] UpdateUserPrivacySettingDto dto, CancellationToken cancellationToken)
    {
        var command = new UpdateUserPrivacyCommand
        {
            IsProfilePublic = dto.IsProfilePublic,
            ShowEmail = dto.ShowEmail,
            ShowPhoneNumber = dto.ShowPhoneNumber,
            ShowGitHubStats = dto.ShowGitHubStats,
            ShowActivityStats = dto.ShowActivityStats
        };

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ResponseModel<UserPrivacySettingDto>.Success(result, "Cập nhật quyền riêng tư thành công."));
    }

    /// <summary>
    /// Xem hồ sơ công khai của thành viên khác (UC-07 - Đã áp dụng Privacy Filter)
    /// </summary>
    [HttpGet("{id:guid}/profile")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseModel<PublicUserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPublicProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUserPublicProfileQuery(id), cancellationToken);
        return Ok(ResponseModel<PublicUserProfileDto>.Success(result));
    }

    /// <summary>
    /// Tải lên và cập nhật ảnh đại diện người dùng lên MinIO S3 (Nén WebP 256x256)
    /// </summary>
    [HttpPost("me/avatar")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ResponseModel<object>.Failure("Vui lòng chọn một tệp hình ảnh hợp lệ."));

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(ResponseModel<object>.Failure("Dung lượng ảnh đại diện không được vượt quá 5MB."));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(ResponseModel<object>.Failure("Định dạng tệp không được hỗ trợ. Vui lòng chọn ảnh JPG, PNG, WEBP hoặc GIF."));

        await using var stream = file.OpenReadStream();
        var command = new UploadAvatarCommand
        {
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType
        };

        var avatarUrl = await Mediator.Send(command, cancellationToken);
        return Ok(ResponseModel<string>.Success(avatarUrl, "Cập nhật ảnh đại diện thành công."));
    }

    /// <summary>
    /// Tải lên ảnh bìa (Cover Banner) trang cá nhân lên MinIO S3 (Nén WebP 1200x400)
    /// </summary>
    [HttpPost("me/cover")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCover(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ResponseModel<object>.Failure("Vui lòng chọn một tệp hình ảnh hợp lệ."));

        if (file.Length > 8 * 1024 * 1024)
            return BadRequest(ResponseModel<object>.Failure("Dung lượng ảnh bìa không được vượt quá 8MB."));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(ResponseModel<object>.Failure("Định dạng tệp không được hỗ trợ. Vui lòng chọn ảnh JPG, PNG, WEBP hoặc GIF."));

        await using var stream = file.OpenReadStream();
        var command = new UploadCoverCommand
        {
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType
        };

        var coverUrl = await Mediator.Send(command, cancellationToken);
        return Ok(ResponseModel<string>.Success(coverUrl, "Cập nhật ảnh bìa thành công."));
    }
}
