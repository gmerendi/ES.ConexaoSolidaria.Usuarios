using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Infrastructure.Services.UserContext
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public SystemUser? GetUser()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            // 1. Caso o usuário não esteja autenticado (Fallback do Sistema)
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // 2. Captura os valores das Claims
            var guidStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            _ = Guid.TryParse(guidStr, out Guid userGuid);

            var nomeCompleto = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("name")?.Value ?? "";

            var cpf = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("cpf")?.Value ?? "";

            var emailStr = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? "";

            // 3. Faz o Parse dos Enums com segurança
            var perfilStr = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("perfil")?.Value ?? "";
            if (!Enum.TryParse(perfilStr, true, out Perfil perfilEnum))
            {
                perfilEnum = Perfil.DOADOR; // Valor padrão caso falhe
            }

            var statusStr = user.FindFirst("status")?.Value ?? "Active";
            if (!Enum.TryParse(statusStr, true, out EntityStatus statusEnum))
            {
                statusEnum = EntityStatus.ACTIVE; // Valor padrão caso falhe
            }

            // 4. Retorna o Usuario do Sistema
            return new SystemUser(
                userGuid,
                nomeCompleto,
                cpf,
                emailStr,
                perfilEnum.ToString(),
                statusEnum.ToString()
            );
        }
    }
}