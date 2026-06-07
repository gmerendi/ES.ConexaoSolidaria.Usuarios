using Usuarios.Domain.Entities.Usuarios;
using Usurios.Domain.Shared.Interfaces;

namespace Usuarios.Domain.Entity.Usuarios
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
        Task<Usuario?> ObterPorCpfAsync(string email, CancellationToken ct = default);
    }
}
