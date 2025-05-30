using System.Text.Json;
using StackExchange.Redis;
using WordsAPI.CacheService;
using System.Threading.Tasks;

namespace WordsAPI.Services;

public class CacheService : ICacheService
{
    private readonly IDatabase _cacheDb;

    public CacheService(IConnectionMultiplexer redis)
    {
        _cacheDb = redis.GetDatabase();
    }
    
    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _cacheDb.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T?>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _cacheDb.StringSetAsync(key, serializedValue, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        await _cacheDb.KeyDeleteAsync(key);
    }

    public async Task ClearCacheAsync(string pattern = "*")
    {
        var endpoints = _cacheDb.Multiplexer.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _cacheDb.Multiplexer.GetServer(endpoint);
            foreach (var key in server.Keys(pattern: pattern))
            {
                await _cacheDb.KeyDeleteAsync(key);
            }
        }
    }
}