using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Usuarios.Api.Extensions
{
    public static class HealthCheckExtension
    {
        public static IServiceCollection AddHealthCheckConfiguration(this IServiceCollection services, ILogger logger)
        {
            services.AddHealthChecks();
            logger.LogInformation(" ***** Health Check service inicializado.");

            return services;
        }





        public static void MapCustomHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = _ => true
            });

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });
        }
    }
}
