namespace Identity.Application.Common.Interfaces;

public interface IRedisCacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task BlacklistTokenAsync(string token, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default);
}
