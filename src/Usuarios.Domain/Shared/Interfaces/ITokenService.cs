using Usuarios.Domain.Entities.Usuarios;

namespace Usuarios.Domain.Shared.Interfaces;

public interface ITokenService
{
    (string Token, DateTime DataExpiracao) GetToken(Usuario usuario);
}   