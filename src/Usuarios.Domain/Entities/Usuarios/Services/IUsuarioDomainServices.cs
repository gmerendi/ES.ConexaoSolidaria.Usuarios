namespace Usuarios.Domain.Entities.Usuarios
{
    public interface IUsuarioDomainService
    {
        void PodeRemoverUsuario(Usuario solicitante, Usuario usuarioAlvo);
        void PodeAlterarUsuario(Usuario solicitante, Usuario usuarioAlvo);
        void PodeAlterarPerfilEStatus(Usuario solicitante, Usuario usuarioAlvo);
    }
}
