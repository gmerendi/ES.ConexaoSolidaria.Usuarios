using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usuarios.Domain.Entities.Usuarios;

namespace Usuarios.Infrastructure.Extensions
{
    public static class DomainServicesExtension
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services, ILogger logger)
        {
            // ApplicationServices
            

            services.AddScoped <IUsuarioDomainService, UsuarioDomainService > ();
            logger.LogInformation(" ***** Domain services inicializados.");

            return services;
        }

    }
}
