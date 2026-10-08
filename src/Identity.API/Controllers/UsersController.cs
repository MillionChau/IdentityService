using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.Users.Commands.DeleteUser;
using Identity.Application.Features.Users.Commands.LockUser;
using Identity.Application.Features.Users.Commands.UnlockUser;
using Identity.Application.Features.Users.Commands.UpdateUser;
using Identity.Application.Features.Users.Queries.GetUserById;
using Identity.Application.Features.Users.Queries.GetUsersByFilter;
using Identity.Domain.Enums;
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
}
