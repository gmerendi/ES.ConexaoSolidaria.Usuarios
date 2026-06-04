using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usuarios.Application.Features.Usuarios;
using Usuarios.Application.Shared;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Extensions
{
    public static class ApplicationServicesExtension
    {
        public static IServiceCollection AddUseCaseServices(this IServiceCollection services, ILogger logger)
        {
            // ApplicationServices
            

            services.AddScoped <IUseCaseHandler<CriarUsuarioCommand, Result<CriarUsuarioResponse>>, CriarUsuarioCommandHandler > ();
            logger.LogInformation(" ***** UseCase services inicializados.");

            return services;
        }

    }
}
