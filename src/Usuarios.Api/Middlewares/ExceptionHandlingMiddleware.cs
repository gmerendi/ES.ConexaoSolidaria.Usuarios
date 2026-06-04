using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Application.Interfaces;
using Usuarios.Domain.Shared.Resources; // Importante para ler do ErrorMessages

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
        var correlationId = correlationIdGenerator.Get() ?? "N/A";

        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogError("[CorrelationId: {CorrelationId}] Erro de negócio detectado: {ErrorCode} - {Message}",
                correlationId, ex.ErrorCode, ex.Message);

            // 1. Extrai o status code (ex: 422, 400, 403) baseado no início do ErrorCode
            var statusCode = ExtrairStatusCode(ex.ErrorCode, HttpStatusCode.UnprocessableEntity);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // 2. Monta o objeto elaborado idêntico ao padrão RFC (ValidationProblemDetails)
            var respostaElaborada = new
            {
                title = "A domain error occurred.", // Ou "Um ou mais erros de negócio ocorreram."
                status = (int)statusCode,
                errors = new Dictionary<string, string[]>
        {
            // Agrupa o erro na chave "Domain" para manter a estrutura de array/dicionário
            { "Domain", new[] { ex.Message } }
        },
                traceId = correlationId,
                codigo = ex.ErrorCode // Mantive o seu código aqui caso o front-end precise dele isolado
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(respostaElaborada));
        }
        catch (BadHttpRequestException ex) // Captura falhas de validação de modelo/Data Annotations
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] Falha na validação dos dados de entrada: {Message}",
                correlationId, ex.Message);

            // Tenta adivinhar se a mensagem enviada é uma chave do resource (ex: "400_NAME_REQUIRED")
            string errorCode = ex.Message;
            string mensagemTraduzida = ErrorMessages.GetString(errorCode);

            var statusCode = ExtrairStatusCode(errorCode, HttpStatusCode.BadRequest);

            await FormatarRespostaErroAsync(context, statusCode, errorCode, mensagemTraduzida);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CorrelationId: {CorrelationId}] Ocorreu um erro não tratado no servidor.", correlationId);

            string codigoErroInesperado = "500_ERRO_INESPERADO";
            string mensagemInesperada = ErrorMessages.GetString(codigoErroInesperado);

            await FormatarRespostaErroAsync(context, HttpStatusCode.InternalServerError, codigoErroInesperado, mensagemInesperada);
        }
    }

    // Método auxiliar para responder à API de forma padronizada
    private static async Task FormatarRespostaErroAsync(HttpContext context, HttpStatusCode statusCode, string errorCode, string mensagem)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Mantive o padrão do seu objeto "erro", mas adicionando o código para o front-end poder mapear se quiser
        var resposta = new
        {
            codigo = errorCode,
            erro = mensagem
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(resposta));
    }

    // Método que lê o "400_" ou "422_" do início da string e converte no Enum do .NET
    private static HttpStatusCode ExtrairStatusCode(string? errorCode, HttpStatusCode fallback)
    {
        if (!string.IsNullOrWhiteSpace(errorCode) && errorCode.Length >= 4)
        {
            var tresPrimeirosCaracteres = errorCode.Substring(0, 3);
            if (int.TryParse(tresPrimeirosCaracteres, out int code))
            {
                return (HttpStatusCode)code;
            }
        }
        return fallback;
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}