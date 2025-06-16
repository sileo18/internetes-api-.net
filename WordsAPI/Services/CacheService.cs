using System.Text.Json;
using StackExchange.Redis;
using WordsAPI.CacheService;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace WordsAPI.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache; // <-- Injeção CORRETA

    public CacheService(IDistributedCache cache) // <-- Construtor CORRETO
    {
        _cache = cache;
    }

    public async Task<T?> GetCacheData<T>(string key)
    {
        var jsonData = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(jsonData))
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(jsonData);
    }

    public async Task SetCacheData<T>(string key, T value, TimeSpan? absoluteExpireTime = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? TimeSpan.FromMinutes(30)
        };
        var jsonData = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, jsonData, options);
    }

    public async Task RemoveCacheData(string key)
    {
        await _cache.RemoveAsync(key);
    }
   }