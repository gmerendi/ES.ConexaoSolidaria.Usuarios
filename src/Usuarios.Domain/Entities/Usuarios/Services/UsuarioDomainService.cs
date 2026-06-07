using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;

namespace Usuarios.Domain.Entities.Usuarios;

public class UsuarioDomainService : IUsuarioDomainService
{
    public void PodeRemoverUsuario(Usuario solicitante, Usuario usuarioAlvo)
    {
        // Pode remover se é Gestor OU se está removendo a si mesmo
        if (solicitante.Perfil == Perfil.GESTOR_ONG || solicitante.Guid == usuarioAlvo.Guid)
        {
            return;
        }

        throw new DomainException("403_USER_CANNOT_REMOVE");
    }

    public void PodeAlterarUsuario(Usuario solicitante, Usuario usuarioAlvo)
    {
        // Regra 1: Gestor sempre pode alterar qualquer um
        if (solicitante.Perfil == Perfil.GESTOR_ONG)
        {
            return;
        }

        // Regra 2: Se for o próprio usuário alterando a si mesmo, valida o status
        if (solicitante.Guid == usuarioAlvo.Guid)
        {
            switch (usuarioAlvo.Status)
            {
                case EntityStatus.SUSPENDED:
                    throw new DomainException("403_USER_SUSPENDED");
                case EntityStatus.REMOVED:
                    throw new DomainException("403_USER_REMOVED");
                default:
                    return; // Status válido (ATIVO, etc), pode continuar
            }
        }

        throw new DomainException("403_USER_CANNOT_ALTER");
    }

    public void PodeAlterarPerfilEStatus(Usuario solicitante, Usuario usuarioAlvo)
    {
        // Regra 1: Apenas administradores/gestores podem alterar perfil ou status
        if (solicitante.Perfil != Perfil.GESTOR_ONG)
        {
            throw new DomainException("403_USER_CANNOT_MODIFY_ACCESS_LEVEL");
        }

        // Regra 2: Admin não pode alterar seu próprio perfil (Segurança) ou status
        if (solicitante.Guid == usuarioAlvo.Guid)
        {
            throw new DomainException("403_USER_CANNOT_MODIFY_OWN_ACCESS_LEVEL");
        }

        return;
    }
}