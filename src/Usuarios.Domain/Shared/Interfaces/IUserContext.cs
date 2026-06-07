using Usuarios.Domain.Entities.Usuarios.DTO;

namespace Usuarios.Domain.Shared.Interfaces;

public interface IUserContext
{
    UsuarioDTO? GetUser();
}