namespace WordsAPI.CacheService;

public interface ICacheService 
{
    Task<T?> GetCacheData<T>(string key);
    Task SetCacheData<T>(string key, T value, TimeSpan? absoluteExpireTime = null);
    Task RemoveCacheData(string key);
}