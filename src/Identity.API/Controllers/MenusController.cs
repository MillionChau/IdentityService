using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.Menus.Queries.GetUserMenus;
using Identity.Application.Features.Permissions.Queries.GetUserPermissions;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MenusController : ApiControllerBase
{
    /// <summary>
    /// Lấy danh sách menu điều hướng cho người dùng (Tương thích endpoint menu-for-user)
    /// </summary>
    [HttpGet("for-user/{userId:guid}")]
    [ProducesResponseType(typeof(ResponseModel<IReadOnlyList<MenuDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenusForUser(Guid userId, CancellationToken cancellationToken)
    {
        var menus = await Mediator.Send(new GetUserMenusQuery(userId), cancellationToken);
        return Ok(ResponseModel<IReadOnlyList<MenuDto>>.Success(menus));
    }

    /// <summary>
    /// Lấy danh sách quyền của người dùng
    /// </summary>
    [HttpGet("permissions/for-user/{userId:guid}")]
    [ProducesResponseType(typeof(ResponseModel<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionsForUser(Guid userId, CancellationToken cancellationToken)
    {
        var permissions = await Mediator.Send(new GetUserPermissionsQuery(userId), cancellationToken);
        return Ok(ResponseModel<IReadOnlyList<PermissionDto>>.Success(permissions));
    }
}
