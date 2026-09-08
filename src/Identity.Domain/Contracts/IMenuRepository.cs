using Identity.Domain.Contracts;
using Identity.Domain.Entities;

namespace Identity.Domain.Contracts;

public interface IMenuRepository : IRepository<Menu>
{
    Task<IReadOnlyList<Menu>> GetMenusForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
