using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Infrastructure.Services.Security;

namespace Usuarios.Infrastructure.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            services.AddScoped<ICryptoService, CryptoService>();
            logger.LogInformation(" ***** CryptoService inicializado.");

            services.AddScoped<ITokenService, TokenService>();
            logger.LogInformation(" ***** TokenService inicializado.");

            return services;
        }
    }
}
