using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Infrastructure.Services.Messaging;

namespace Usuarios.Infrastructure.Extensions
{
    public static class MessagingExtensions
    {

        public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            services.AddScoped<IMessageService, MessageService>();

            var applicationType = configuration["ApplicationType"] ?? "LOCAL";

            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    // Mantém na memória do app se o Rabbit cair
                    cfg.UseInMemoryOutbox();

                    // Use os nomes das chaves conforme appsettings.json
                    var host = configuration["RabbitMq:Host"];
                    var user = configuration["RabbitMq:Username"];
                    var pass = configuration["RabbitMq:Password"];

                    if (applicationType == "LOCAL" || applicationType == "LAB")
                    {
                        cfg.Host(host, "/", h =>
                        {
                            h.Username(user);
                            h.Password(pass);
                        });
                    }
                    else if (applicationType == "AWS")
                    {
                        cfg.Host(new Uri(host), "/", h =>
                        {
                            h.Username(user);
                            h.Password(pass);
                        });
                    }


                    cfg.ConfigureEndpoints(context);
                });
            });



            logger.LogInformation(" ***** Masstransit service inicializado.");

            return services;
        }
    }
}

       