using Identity.Domain.Contracts;
using Identity.Domain.Entities;

namespace Identity.Domain.Contracts;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
