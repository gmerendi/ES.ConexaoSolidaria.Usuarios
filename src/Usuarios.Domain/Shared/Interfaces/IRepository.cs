using Usuarios.Domain.Shared.Entity;

namespace Usurios.Domain.Shared.Interfaces
{
    public interface IRepository<T> where T : EntityBase
    {
        Task<IList<T>> ObterTodosAsync(CancellationToken cancellationToken = default);
        Task<T?> ObterPorGuidAsync(Guid guid, CancellationToken cancellationToken = default);
        Task CadastrarAsync(T entidade, CancellationToken cancellationToken = default);
        Task AlterarAsync(T entity, CancellationToken cancellationToken = default);
        Task RemoverAsync(Guid guid, CancellationToken cancellationToken = default);
    }
}