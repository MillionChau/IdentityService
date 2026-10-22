using System.Text.Json;
using Identity.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Identity.Infrastructure.Services;

public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(ILogger<RedisCacheService> logger, IConnectionMultiplexer? redis = null)
    {
        _logger = logger;
        _redis = redis;
    }

    private IDatabase? GetDatabase()
    {
        try
        {
            if (_redis != null && _redis.IsConnected)
            {
                return _redis.GetDatabase();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Redis database.");
        }
        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        if (db == null) return;

        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            if (expiry.HasValue)
            {
                await db.StringSetAsync(key, json, expiry.Value);
            }
            else
            {
                await db.StringSetAsync(key, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Redis cache key: {Key}", key);
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        if (db == null) return default;

        try
        {
            var value = await db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;

            return JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis cache key: {Key}", key);
            return default;
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        if (db == null) return false;

        try
        {
            return await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Redis cache key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        if (db == null) return false;

        try
        {
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Redis cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task BlacklistTokenAsync(string token, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var key = $"blacklist:{token}";
        await SetAsync(key, "revoked", expiry, cancellationToken);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var key = $"blacklist:{token}";
        return await ExistsAsync(key, cancellationToken);
    }
}
