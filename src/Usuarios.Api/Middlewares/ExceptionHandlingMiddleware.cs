using System.Net;
using System.Text.Json;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Application.Interfaces;

namespace Usuarios.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            var correlationId = correlationIdGenerator.Get() ?? "N/A";

            _logger.LogError("[CorrelationId: {CorrelationId}] Erro de negócio detectado: {ErrorCode} - {Message}",
                correlationId, ex.ErrorCode, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;

            var resposta = new { erro = ex.Message };

            await context.Response.WriteAsync(JsonSerializer.Serialize(resposta));
        }
        catch (Exception ex)
        {
            var correlationId = correlationIdGenerator.Get() ?? "N/A";

            _logger.LogError(ex, "[CorrelationId: {CorrelationId}] Ocorreu um erro não tratado no servidor.", correlationId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var resposta = new { erro = "Ocorreu um erro interno inesperado no servidor." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(resposta));
        }
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}