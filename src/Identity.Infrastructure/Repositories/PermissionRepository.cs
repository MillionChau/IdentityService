using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
{
    public PermissionRepository(IdentityDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rolePermissions = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission))
            .Distinct()
            .ToListAsync(cancellationToken);

        return rolePermissions;
    }
}
