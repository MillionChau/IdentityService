using Identity.Domain.Contracts;
using Identity.Domain.Entities;

namespace Identity.Domain.Contracts;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalLoginAsync(Domain.Enums.AuthProvider provider, string providerKey, CancellationToken cancellationToken = default);
    Task<User?> GetUserWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetUserWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserNameOrEmailAsync(string userName, string email, CancellationToken cancellationToken = default);
}
