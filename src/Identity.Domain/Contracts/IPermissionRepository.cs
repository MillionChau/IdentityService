using Identity.Domain.Contracts;
using Identity.Domain.Entities;

namespace Identity.Domain.Contracts;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<IReadOnlyList<Permission>> GetPermissionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
