using Usuarios.Domain.Shared.Primitives;

public interface IUserContext
{
    SystemUser? GetUser();
}