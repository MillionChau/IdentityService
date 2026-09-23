using System.Security.Claims;
using Identity.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(ClaimTypes.Sid)
                ?? user.FindFirstValue("sub")
                ?? user.FindFirstValue("user_id");

            if (Guid.TryParse(idClaim, out var guid))
            {
                return guid;
            }

            // Check custom header if behind gateway
            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("X-User-Id", out var headerId) == true &&
                Guid.TryParse(headerId, out var parsedHeaderId))
            {
                return parsedHeaderId;
            }

            return null;
        }
    }

    public string? UserName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var name = user?.FindFirstValue(ClaimTypes.Name)
                ?? user?.FindFirstValue("preferred_username")
                ?? user?.FindFirstValue("username")
                ?? user?.FindFirstValue("sub");

            if (!string.IsNullOrEmpty(name)) return name;

            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("X-User-Username", out var headerName) == true)
            {
                return headerName.ToString();
            }

            return null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public IReadOnlyList<string> Roles =>
        _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? new List<string>();

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true ||
        !string.IsNullOrEmpty(UserName);
}
