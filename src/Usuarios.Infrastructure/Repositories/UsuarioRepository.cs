using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entity.Usuarios;
using Usuarios.Infrastructure.Data;

namespace Usuarios.Infrastructure.Repositories;

public class UsuarioRepository : EFRepository<Usuario>, IUsuarioRepository
{
    private readonly string _connectionString;

    public UsuarioRepository(ApplicationDbContext context, IConfiguration configuration) : base(context)
    {
        _connectionString = configuration.GetConnectionString("ConnectionString") ?? "";
    }





    public async Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.Endereco.ToLower() == email.ToLower(), ct);
    }


    public async Task<Usuario?> ObterPorCpfAsync(string cpf, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Cpf.Numero.ToLower() == cpf.ToLower(), ct);
    }
}
