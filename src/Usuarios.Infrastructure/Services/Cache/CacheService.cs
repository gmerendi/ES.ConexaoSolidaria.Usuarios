using StackExchange.Redis;
using System.Text.Json;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Interfaces;

namespace Usuarios.Infrastructure.Services.Cache
{
    public class CacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IBaseLogger<CacheService> _logger;
        private const string BlacklistPrefix = "blacklist:";

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };



        public CacheService(IConnectionMultiplexer redis, IBaseLogger<CacheService> logger)
        {
            _redis = redis;
            _logger = logger;
        }



        public async Task SetBlacklistAsync(string token, TimeSpan expiration, CancellationToken ct = default)
        {
            string key = $"{BlacklistPrefix}{token}";
            await SetAsync(key, "revogado", expiration);
            _logger.LogInformation("Token adicionado à Blacklist.", BaseLogType.LOG, new { Key = key });
        }



        public async Task<bool> IsBlacklistedAsync(string token, CancellationToken ct = default)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis não está conectado - falha ao checar blacklist.", BaseLogType.LOG);
                    return true;
                }

                string key = $"{BlacklistPrefix}{token}";
                var db = _redis.GetDatabase();
                bool exists = await db.KeyExistsAsync(key);

                if (exists)
                    _logger.LogWarning("Tentativa de acesso com token na Blacklist.", BaseLogType.LOG, new { Key = key });

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao checar blacklist no Redis.", BaseLogType.LOG, ex);
                return true;
            }
        }


        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis não está conectado - tentativa de gravação.", BaseLogType.LOG, new { Key = key });
                    return;
                }

                var db = _redis.GetDatabase();
                string json = value is string strValue ? strValue : JsonSerializer.Serialize(value, _jsonOptions);
                await db.StringSetAsync(key, json, expiration);
                _logger.LogInformation("Dado gravado no Redis.", BaseLogType.LOG, new { Key = key });
            }
            catch (Exception ex)
            {
                _logger.LogError("Redis indisponível.", BaseLogType.LOG, ex);
            }
        }


        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis não está conectado - tentativa de busca.", BaseLogType.LOG, new { Key = key });
                    return default;
                }

                var db = _redis.GetDatabase();
                var data = await db.StringGetAsync(key);
                _logger.LogInformation("Dado retornado do Redis.", BaseLogType.LOG, new { Key = key });

                if (data.IsNullOrEmpty) return default;

                if (typeof(T) == typeof(string))
                    return (T)(object)data.ToString();

                return JsonSerializer.Deserialize<T>(data!, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao deserializar chave do Redis: {Key}", BaseLogType.LOG, ex, new { Key = key });
                return default;
            }
        }


        public async Task RemoveAsync(string key)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis não está conectado - tentativa de remoção.", BaseLogType.LOG, new { Key = key });
                    return;
                }

                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(key);
                _logger.LogInformation("Dado removido do Redis: {Key}", BaseLogType.LOG, new { Key = key });
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao remover chave do Redis.", BaseLogType.LOG, ex);
            }
        }



        public async Task RemoveByPrefixAsync(string prefix)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();

                if (!keys.Any())
                {
                    _logger.LogInformation("Nenhuma key encontrada com prefixo: {Prefix}", BaseLogType.LOG, new { Prefix = prefix });
                    return;
                }

                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(keys);
                _logger.LogInformation("{Count} keys removidas com prefixo: {Prefix}", BaseLogType.LOG, new { Count = keys.Length, Prefix = prefix });
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao remover keys com prefixo: {Prefix}", BaseLogType.LOG, ex, new { Prefix = prefix });
            }
        }
    }
}