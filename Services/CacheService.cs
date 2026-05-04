using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AIChatApp.Services;

public class CacheService
{
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SetAsync(string key, object value, int minutes = 10)
    {
        var json = JsonSerializer.Serialize(value);

        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes)
        });
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);

        return data == null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}