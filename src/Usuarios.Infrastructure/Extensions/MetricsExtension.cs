using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Infrastructure.Services.Metrics;

namespace Usuarios.Infrastructure.Extensions;

public static class MetricsExtension
{
    /// <summary>
    /// Adiciona IMetricsService ao container. Chamado em Program.cs junto às outras extensions.
    /// </summary>
    public static IServiceCollection AddMetricsServices(this IServiceCollection services, ILogger logger)
    {
        services.AddSingleton<IMetricsService, MetricsService>();
        logger.LogInformation(" ***** Metrics service (Prometheus) inicializado.");
        return services;
    }
}
