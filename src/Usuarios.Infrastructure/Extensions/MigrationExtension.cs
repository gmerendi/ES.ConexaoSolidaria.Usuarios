using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Usuarios.Infrastructure.Data;

namespace Usuarios.Infrastructure.Extensions
{
    public static class MigrationExtensions
    {
        public static void ApplyMigrations(this IHost host, ILogger logger)
        {
            logger.LogInformation(" ***** Migrations - Application - Inicializado.");
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                // 1. Migration para ApplicationDbContext
                try
                {
                    var dbContext = services.GetRequiredService<ApplicationDbContext>();
                    dbContext.Database.Migrate();
                    logger.LogInformation(" ***** ✅ - Migrations aplicadas com sucesso - Application");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, " ***** ⚠️ - Erro ao aplicar as migrations - Application");
                }

                // 2. Migration para DynamoDB
                try
                {
                    Infrastructure.Migrations.DynamoDbConfiguration.DynamoDbMigration(services).GetAwaiter().GetResult();
                    logger.LogInformation(" ***** ✅ - Migrations aplicadas com sucesso - DynamoDB");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, " ***** ⚠️ - Erro ao aplicar as migrations - DynamoDB");
                }

            }
        }

    }
}
