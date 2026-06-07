using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usuarios.Application.Features.Auth;
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
            

            services.AddScoped<IUseCaseHandler<CriarUsuarioCommand, Result<CriarUsuarioResponse>>, CriarUsuarioCommandHandler>();
            services.AddScoped<IUseCaseHandler<ObterUsuarioCommand, Result<ObterUsuarioResponse>>, ObterUsuarioCommandHandler>();
            services.AddScoped<IUseCaseHandler<LogarUsuarioCommand, Result<LogarUsuarioResponse>>, LogarUsuarioCommandHandler>();
            services.AddScoped<IUseCaseHandler<DeslogarUsuarioCommand, Result<bool>>, DeslogarUsuarioCommandHandler>();
            services.AddScoped<IUseCaseHandler<RemoverUsuarioCommand, Result<bool>>, RemoverUsuarioCommandHandler>();
            services.AddScoped<IUseCaseHandler<ResetarSenhaCommand, Result<string>>, ResetarSenhaCommandHandler>();
            services.AddScoped<IUseCaseHandler<SuspenderUsuarioCommand, Result<bool>>, SuspenderUsuarioCommandHandler>();
            services.AddScoped<IUseCaseHandler<AtivarUsuarioCommand, Result<bool>>, AtivarUsuarioCommandHandler>();
            services.AddScoped<IUseCaseHandler<AlterarPerfilParaGestorCommand, Result<bool>>, AlterarPerfilParaGestorCommandHandler>();
            services.AddScoped<IUseCaseHandler<AlterarPerfilParaDoadorCommand, Result<bool>>, AlterarPerfilParaDoadorCommandHandler>();
            services.AddScoped<IUseCaseHandler<ModificarUsuarioCommand, Result<ModificarUsuarioResponse>>, ModificarUsuarioCommandHandler>();

            logger.LogInformation(" ***** UseCase services inicializados.");

            return services;
        }

    }
}
