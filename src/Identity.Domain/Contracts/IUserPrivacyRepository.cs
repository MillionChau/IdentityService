using Identity.Domain.Entities;

namespace Identity.Domain.Contracts;

public interface IUserPrivacyRepository : IRepository<UserPrivacySetting>
{
    Task<UserPrivacySetting?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
