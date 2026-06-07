using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Infrastructure.Services.Cache;

namespace Usuarios.Infrastructure.Extensions
{
    public static class CacheExtensions
    {
        public static IServiceCollection AddCacheService(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            var redisConn = configuration.GetConnectionString("Redis");

            services.AddSingleton<IConnectionMultiplexer>(sp => {
                var options = ConfigurationOptions.Parse(redisConn);
                options.AbortOnConnectFail = false; // Importante para não quebrar a API se o Redis sumir
                options.ConnectRetry = 3;
                return ConnectionMultiplexer.Connect(options);
            });

            services.AddScoped<ICacheService, CacheService>();
            logger.LogInformation(" ***** CacheService inicializado.");

            return services;
            
        }
    }
}
