using Microsoft.EntityFrameworkCore;
using Usuarios.Domain.Shared.Entity;
using Usuarios.Infrastructure.Data;
using Usurios.Domain.Shared.Interfaces;

namespace Usuarios.Infrastructure.Repositories
{
    public class EFRepository<T> : IRepository<T> where T : EntityBase
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public EFRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task AlterarAsync(T entidade, CancellationToken cancellationToken = default)
        {
            entidade.DataModificacao = DateTime.UtcNow;

            _context.Entry(entidade).State = EntityState.Modified;
            try
            {
                // Executa a persistência de forma assíncrona 🚀
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Erro ao alterar: {message}", ex);
            }
        }

        public async Task CadastrarAsync(T entidade, CancellationToken cancellationToken = default)
        {
            entidade.DataCriacao = DateTime.UtcNow;

            // O EF possui o AddAsync para preparar a árvore de entidades assincronamente se necessário
            await _dbSet.AddAsync(entidade, cancellationToken);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Erro ao cadastrar: {message}", ex);
            }
        }

        public async Task RemoverAsync(Guid guid, CancellationToken cancellationToken = default)
        {
            // Busca a entidade usando o método assíncrono antes de deletar
            var entidade = await ObterPorGuidAsync(guid, cancellationToken);

            if (entidade != null)
            {
                _dbSet.Remove(entidade);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    var message = ex.InnerException?.Message ?? ex.Message;
                    throw new Exception($"Erro ao remover: {message}", ex);
                }
            }
        }

        public async Task<T?> ObterPorGuidAsync(Guid guid, CancellationToken cancellationToken = default)
        {
            // Evita travar a thread esperando a resposta de busca do banco
            return await _dbSet.FirstOrDefaultAsync(e => e.Guid == guid, cancellationToken);
        }

        public async Task<IList<T>> ObterTodosAsync(CancellationToken cancellationToken = default)
        {
            // Carrega a lista inteira do banco de forma assíncrona
            return await _dbSet.ToListAsync(cancellationToken);
        }
    }
}