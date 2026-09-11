using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IdentityDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task RevokeByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbSet.Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsUsed).ToListAsync(cancellationToken);
        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
        }
    }
}
