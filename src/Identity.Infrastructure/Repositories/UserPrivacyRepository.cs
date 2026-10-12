using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class UserPrivacyRepository : BaseRepository<UserPrivacySetting>, IUserPrivacyRepository
{
    public UserPrivacyRepository(IdentityDbContext context) : base(context)
    {
    }

    public async Task<UserPrivacySetting?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
