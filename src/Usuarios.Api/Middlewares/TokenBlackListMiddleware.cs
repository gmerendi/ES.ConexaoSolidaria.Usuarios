using System.Net;
using Usuarios.Domain.Shared.Interfaces;

namespace Usuarios.Api.Middlewares;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICacheService cacheService)
    {
        // 1. Extrai o token do cabeçalho Authorization
        string authHeader = context.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();

            // 2. CHECAGEM NO REDIS: O token está na lista negra?
            bool isBlacklisted = await cacheService.IsBlacklistedAsync(token);

            if (isBlacklisted)
            {
                // Se estiver na lista negra, barra imediatamente com 401 Unauthorized
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "Token revogado. Por favor faça login novamente" });
                return; // Corta o fluxo aqui, não deixa ir para o Controller/Handler
            }
        }

        await _next(context);
    }
}


public static class TokenBlacklistMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenBlacklistMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenBlacklistMiddleware>();
    }
}