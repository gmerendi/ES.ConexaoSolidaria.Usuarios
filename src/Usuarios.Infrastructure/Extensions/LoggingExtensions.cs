using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Infrastructure.Services.Logging;

namespace Usuarios.Infrastructure.Extensions
{
    public static class LoggingExtensions
    {
        public static IServiceCollection AddCustomLogging(this IServiceCollection services, ILogger logger)
        {
            services.AddTransient<ICorrelationIdGenerator, CorrelationIdGenerator>();
            logger.LogInformation(" ***** CorrelationIdGenerator service inicializado.");
            
            services.AddTransient(typeof(IBaseLogger<>), typeof(BaseLogger<>));
            logger.LogInformation(" ***** BaseLogger service inicializado.");

            return services;
        }
    }
}
