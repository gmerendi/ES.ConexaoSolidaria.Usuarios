using Microsoft.AspNetCore.Mvc;
using Usuarios.Domain.Shared.Resources;

namespace Usuarios.Api.Configuration;

public static class MvcConfigurationExtensions
{
    public static IMvcBuilder ConfigurarMensagensDeValidacaoCustomizadas(this IMvcBuilder builder)
    {
        return builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errosTraduzidos = context.ModelState.Keys
                    .Where(key => context.ModelState[key]?.Errors.Any() == true)
                    .ToDictionary(
                        key => key,
                        key => context.ModelState[key]!.Errors
                            .Select(error => ErrorMessages.GetString(error.ErrorMessage))
                            .ToList()
                    );

                var resposta = new
                {
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = errosTraduzidos,
                    traceId = context.HttpContext.TraceIdentifier
                };

                return new BadRequestObjectResult(resposta);
            };
        });
    }
}