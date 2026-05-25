using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PediMix.Application.Interfaces;

namespace PediMix.Infrastructure.Services;

/// <summary>
/// Cache híbrido: tenta Redis (IDistributedCache) primeiro; se indisponível,
/// usa IMemoryCache como fallback. Falhas são silenciadas — a API continua
/// funcionando mesmo sem cache.
/// </summary>
public class MusicCacheService : IMusicCacheService
{
    private readonly IDistributedCache? _redis;
    private readonly IMemoryCache _memory;
    private readonly ILogger<MusicCacheService> _logger;
    private readonly bool _redisEnabled;
    private bool _redisAvailable;
    private DateTime _redisNextProbe = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MusicCacheService(
        IMemoryCache memory,
        ILogger<MusicCacheService> logger,
        IDistributedCache? redis = null)
    {
        _memory = memory;
        _logger = logger;
        _redis = redis;
        _redisEnabled = redis is not null;
        _redisAvailable = _redisEnabled;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        // 1. Tenta Redis (se habilitado e considerado disponível)
        if (UseRedis())
        {
            try
            {
                var data = await _redis!.GetStringAsync(key);
                if (!string.IsNullOrEmpty(data))
                    return JsonSerializer.Deserialize<T>(data, JsonOpts);
            }
            catch (Exception ex)
            {
                MarkRedisDown(ex, "GET", key);
            }
        }

        // 2. Fallback: memória
        if (_memory.TryGetValue<T>(key, out var cached))
            return cached;

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        // Sempre grava na memória — é barato e serve como segundo nível
        _memory.Set(key, value, expiration);

        if (UseRedis())
        {
            try
            {
                var data = JsonSerializer.Serialize(value, JsonOpts);
                await _redis!.SetStringAsync(key, data, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });
            }
            catch (Exception ex)
            {
                MarkRedisDown(ex, "SET", key);
            }
        }
    }

    public async Task RemoveAsync(string key)
    {
        _memory.Remove(key);

        if (UseRedis())
        {
            try { await _redis!.RemoveAsync(key); }
            catch (Exception ex) { MarkRedisDown(ex, "REMOVE", key); }
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (UseRedis())
        {
            try
            {
                var data = await _redis!.GetStringAsync(key);
                if (!string.IsNullOrEmpty(data)) return true;
            }
            catch (Exception ex)
            {
                MarkRedisDown(ex, "EXISTS", key);
            }
        }

        return _memory.TryGetValue(key, out _);
    }

    // ------------------------------------------------------------
    // Resiliência: se Redis falhar, marca como indisponível por 30s
    // e volta a tentar depois. Evita logar erro em todo request.
    // ------------------------------------------------------------
    private bool UseRedis()
    {
        if (!_redisEnabled) return false;
        if (_redisAvailable) return true;
        if (DateTime.UtcNow >= _redisNextProbe)
        {
            _redisAvailable = true; // probe — se falhar de novo, será marcado off
            return true;
        }
        return false;
    }

    private void MarkRedisDown(Exception ex, string op, string key)
    {
        _redisAvailable = false;
        _redisNextProbe = DateTime.UtcNow.AddSeconds(30);
        _logger.LogWarning(ex,
            "[MusicCache] Redis {Op} falhou para key={Key}. Caindo para IMemoryCache por 30s.",
            op, key);
    }
}
