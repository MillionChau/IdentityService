using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class MenuRepository : BaseRepository<Menu>, IMenuRepository
{
    public MenuRepository(IdentityDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Menu>> GetMenusForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var permissions = (await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allMenus = await _dbSet
            .Include(m => m.Children)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);

        // Filter menus matching user permissions or having no required permission
        var rootMenus = allMenus
            .Where(m => m.ParentId == null && (string.IsNullOrEmpty(m.RequiredPermission) || permissions.Contains(m.RequiredPermission)))
            .ToList();

        return rootMenus;
    }
}
