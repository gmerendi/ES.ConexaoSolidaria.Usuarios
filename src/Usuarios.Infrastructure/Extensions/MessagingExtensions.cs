using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Infrastructure.Services.Messaging;
using Amazon.SQS;
using System;

namespace Usuarios.Infrastructure.Extensions
{
    public static class MessagingExtensions
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            services.AddScoped<IMessageService, MessageService>();

            var applicationType = Environment.GetEnvironmentVariable("Application__Type") ?? configuration["ApplicationType"] ?? "LOCAL";

            services.AddMassTransit(x =>
            {
                if (applicationType == "LOCAL")
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        // Mantém na memória do app se o Rabbit cair
                        cfg.UseInMemoryOutbox();

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
                }
                else
                {
                    // Registra o MassTransit Em Memória para o ambiente Cloud
                    // Isso registra o IBus no container, evitando o erro de DI, mas SEM buscar o RabbitMQ!
                    logger.LogInformation(" ***** MassTransit: Configurando transporte In-Memory (Ambiente Cloud).");
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                }
            });

            logger.LogInformation(" ***** Masstransit service inicializado.");

            if (applicationType == "LOCAL")
            {
                logger.LogInformation(" ***** 🛠️ - Ambiente LOCAL: Configurando cliente SQS dummy.");

                var dummyOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
                {
                    Credentials = new Amazon.Runtime.BasicAWSCredentials("ignore", "ignore"),
                    Region = Amazon.RegionEndpoint.USEast1
                };

                services.AddDefaultAWSOptions(dummyOptions);
                services.AddAWSService<IAmazonSQS>();
            }
                

            if (applicationType == "AWS" || applicationType == "LAB")
            {
                logger.LogInformation(" ***** 🛠️ - Ambiente AWS/LAB: Configurando cliente SQS de forma explícita.");

                // Força a leitura das credenciais que o seu Secret do Kubernetes injetou no container
                var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
                var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
                var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";

                var config = new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
                Amazon.Runtime.AWSCredentials credentials;

                if (!string.IsNullOrEmpty(sessionToken))
                {
                    credentials = new Amazon.Runtime.SessionAWSCredentials(accessKey, secretKey, sessionToken);
                }
                else
                {
                    credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
                }

                // Injeta o cliente do SQS montado cirurgicamente com o Token
                services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(credentials, config));
            }

            return services;
        }
    }
}