using Microsoft.AspNetCore.Mvc;
using Usuarios.Application.Interfaces;
using Usuarios.Domain.Shared.Resources;

namespace Usuarios.Api.Extensions;

public static class MvcBehaviorExtensions
{
    public static IMvcBuilder ConfigurarErrosDeValidacaoCustomizados(this IMvcBuilder builder)
    {
        return builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                // 1. Recupera o seu gerador de CorrelationId a partir do escopo do request
                var correlationIdGenerator = context.HttpContext.RequestServices
                    .GetRequiredService<ICorrelationIdGenerator>();

                var correlationId = correlationIdGenerator.Get() ?? "N/A";

                // 2. Sincroniza o TraceIdentifier nativo do .NET com o seu CorrelationId
                context.HttpContext.TraceIdentifier = correlationId;

                // 3. Mapeia e traduz os erros do ModelState usando o seu Resource
                var errosTraduzidos = context.ModelState.Keys
                    .Where(key => context.ModelState[key]?.Errors.Any() == true)
                    .ToDictionary(
                        key => key,
                        key => context.ModelState[key]!.Errors
                            .Select(error => ErrorMessages.GetString(error.ErrorMessage))
                            .ToList()
                    );

                // 4. Monta a estrutura rica/elaborada idêntica ao padrão RFC
                var respostaCustomizada = new
                {
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = errosTraduzidos,
                    traceId = correlationId
                };

                return new BadRequestObjectResult(respostaCustomizada);
            };
        });
    }
}