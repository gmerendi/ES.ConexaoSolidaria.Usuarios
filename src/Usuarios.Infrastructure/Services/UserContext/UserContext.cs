using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entities.Usuarios.DTO;
using Usuarios.Domain.Enums;

namespace Usuarios.Infrastructure.Services.UserContext
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public UsuarioDTO? GetUser()
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

            var nomeCompleto = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("name")?.Value ?? "Unknown";

            var cpfStr = user.FindFirst("cpf")?.Value ?? "00000000000";
            var emailStr = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? "unknown@fcg.internal";

            // 3. Faz o Parse dos Enums com segurança
            var perfilStr = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("perfil")?.Value ?? "User";
            if (!Enum.TryParse(perfilStr, true, out Perfil perfilEnum))
            {
                perfilEnum = Perfil.DOADOR; // Valor padrão caso falhe
            }

            var statusStr = user.FindFirst("status")?.Value ?? "Active";
            if (!Enum.TryParse(statusStr, true, out EntityStatus statusEnum))
            {
                statusEnum = EntityStatus.ACTIVE; // Valor padrão caso falhe
            }

            // 4. Retorna o DTO utilizando o construtor que você definiu
            return new UsuarioDTO(
                userGuid,
                nomeCompleto,
                Cpf.Create(cpfStr), 
                Email.Create(emailStr),
                perfilEnum,
                statusEnum
            );
        }
    }
}