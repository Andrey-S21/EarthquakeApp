using Microsoft.Extensions.Caching.Memory;

namespace EarthquakeApp.Api.Caching;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            _logger.LogInformation("Cache HIT: {Key}", key);
            return cached;
        }

        _logger.LogInformation("Cache MISS: {Key}", key);
        var value = await factory();

        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });

        return value;
    }
}
