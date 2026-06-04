using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Usuarios.Api.Extensions
{
    public static class SwaggerExtension
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, ILogger logger)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ONG Esperança Solidária - Conexao Solidaria - Usuarios.API ",
                    Version = "V.1.0.0",
                    Description = "## Tech Challenge - Fase 5\nMVP da plataforma digital Conexao solidaria. Esta API gerencia usuários e autenticação.",
                    Contact = new OpenApiContact
                    {
                        Name = "Grupo 1 (Gustavo Merendi, Thiago Galante)",
                        Email = "postech.grupo39@gmail.com"
                    }
                });

                // Configurar comentários XML
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Configuração JWT no Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Insira o token JWT",
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            logger.LogInformation(" ***** Swagger service inicializado.");

            return services;
        }




        public static IApplicationBuilder UseSwaggerMiddleware(this IApplicationBuilder app, ILogger logger)
        {
            app.UseSwagger(options =>
            {
                options.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    logger.LogInformation(" ***** Swagger Configuration Started (Proxy/Gateway)");

                    var forwardedHost = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault();
                    var forwardedProto = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault();
                    var forwardedPrefix = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault();

                    if (!string.IsNullOrEmpty(forwardedHost))
                    {
                        var serverUrl = $"{forwardedProto ?? "https"}://{forwardedHost}{forwardedPrefix}";
                        swagger.Servers = new List<OpenApiServer> { new() { Url = serverUrl, Description = "API Gateway" } };
                        logger.LogInformation(" ***** Swagger URL configured via Forwarded Headers: {Url}", serverUrl);
                    }
                    else
                    {
                        var fallbackUrl = $"{httpReq.Scheme}://{httpReq.Host.Value}";
                        swagger.Servers = new List<OpenApiServer> { new() { Url = fallbackUrl, Description = "Local/Internal" } };
                        logger.LogInformation(" ***** Swagger URL fallback: {Url}", fallbackUrl);
                    }
                    logger.LogInformation(" ***** Swagger Configuration Finished (Proxy/Gateway)");
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "Conexao Solidaria - Usuarios.Api");
                c.RoutePrefix = "swagger";
            });

            return app;
        }
    }
}
