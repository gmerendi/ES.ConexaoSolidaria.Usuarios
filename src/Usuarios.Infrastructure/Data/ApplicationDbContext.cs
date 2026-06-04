using Microsoft.EntityFrameworkCore;
using Usuarios.Domain.Entities.Usuarios;

namespace Usuarios.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public ApplicationDbContext()
        {
        }




        /****** DbSets ******/
        public DbSet<Usuario> Usuario { get; set; }






        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? connectionString = null;
            if (optionsBuilder.IsConfigured)
                return;

            try
            {

            }
            catch { /* fallback */ }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("ERRO: A Connection String 'ConnectionString' não foi encontrada nem no appsettings nem nas Variáveis de Ambiente.");
            }

            optionsBuilder.UseNpgsql(connectionString);
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            modelBuilder.HasDefaultSchema("identidade");
        }
    }
}
