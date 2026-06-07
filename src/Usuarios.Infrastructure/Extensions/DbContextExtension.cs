using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usuarios.Infrastructure.Data;

namespace Usuarios.Infrastructure.Extensions
{
    public static class DbContextExtensions
    {
        public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            var connectionString = configuration.GetConnectionString("Database");

            if (string.IsNullOrEmpty(connectionString))
                logger.LogWarning(" ***** ⚠️ - Connection String (Application) nula ou vazia");
            else
                logger.LogInformation(" ***** ✅ - Connection String (Application) encontrada");

            // DbContext Principal com Interceptor de Auditoria
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseNpgsql(connectionString);
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            }, ServiceLifetime.Scoped);

            logger.LogInformation(" ***** DbContext service inicializado.");

            return services;
        }
    }
}
