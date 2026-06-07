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

        // ✅ Criamos as opções globais de serialização para aceitar PascalCase e camelCase
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
            _logger.LogInformation("Token adicionado com sucesso à Blacklist.", BaseLogType.LOG, key);
        }

        public async Task<bool> IsBlacklistedAsync(string token, CancellationToken ct = default)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis não está conectado - Falha ao checar blacklist", BaseLogType.LOG, token);
                    return true;
                }

                string key = $"{BlacklistPrefix}{token}";
                var db = _redis.GetDatabase();
                bool exists = await db.KeyExistsAsync(key);

                if (exists)
                {
                    _logger.LogWarning("Tentativa de acesso com Token que está na Blacklist!", BaseLogType.LOG, key);
                }

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao checar blacklist no Redis: " + ex.Message, BaseLogType.LOG, ex);
                return true;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis não esta conectado - tentativa gravacao", BaseLogType.LOG, value);
                    return;
                }
                var db = _redis.GetDatabase();

                // Se o valor já for uma string, não precisamos serializar (evita aspas duplas extras)
                string json = value is string strValue ? strValue : JsonSerializer.Serialize(value, _jsonOptions);

                await db.StringSetAsync(key, json, expiration);
                _logger.LogInformation("Dado gravado no Redis.", BaseLogType.LOG, value);
            }
            catch (Exception ex)
            {
                _logger.LogError("Redis Indisponível: " + ex.Message, BaseLogType.LOG, ex);
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis não esta conectado - tentativa busca", BaseLogType.LOG, key);
                    return default;
                }
                var db = _redis.GetDatabase();
                var data = await db.StringGetAsync(key);

                _logger.LogInformation("Dado retornado do Redis", BaseLogType.LOG, data);

                if (data.IsNullOrEmpty) return default;

                // ✅ SE O TIPO SOLICITADO FOR STRING: Retorna o texto bruto diretamente
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)data.ToString();
                }

                // ✅ SE FOR OBJETO: Deserializa usando as opções de Case Insensitive
                return JsonSerializer.Deserialize<T>(data!, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao deserializar chave {key}: {ex.Message}", BaseLogType.LOG, ex);
                return default;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis não esta conectado - tentativa remocao", BaseLogType.LOG, key);
                    return;
                }
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(key);
                _logger.LogInformation("Dado removido do Redis: " + key, BaseLogType.LOG, key);
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao remover chave do Redis: " + ex.Message, BaseLogType.LOG, ex);
            }
        }
    }
}