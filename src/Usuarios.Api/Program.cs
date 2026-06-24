using Prometheus;
using Usuarios.Api.Configuration;
using Usuarios.Api.Extensions;
using Usuarios.Api.Middlewares;
using Usuarios.Application.Extensions;
using Usuarios.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
var logCounter = 0;
var logTotal = 8;

// ──────────────────────────────────────────────────────────────────────────────
// ── Configuration 
// ──────────────────────────────────────────────────────────────────────────────
ConfigureAppSettings(builder.Configuration);


// ──────────────────────────────────────────────────────────────────────────────
// ── Logs
// ──────────────────────────────────────────────────────────────────────────────
using var loggerFactory = LoggerFactory.Create(logging => {
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    logging.AddSimpleConsole(); 
});
var logger = loggerFactory.CreateLogger("Program");
logger.LogInformation(" ***** Inicializando Users API ");
logCounter++;


// ──────────────────────────────────────────────────────────────────────────────
// ── 1. Api
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Endpoints ", logCounter, logTotal);
builder.Services.AddControllers().ConfigurarMensagensDeValidacaoCustomizadas().ConfigurarErrosDeValidacaoCustomizados();
builder.Services.AddEndpointsApiExplorer();
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Endpoints ", logCounter++, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── 2. Api Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Api Extensions ", logCounter, logTotal);
builder.Services.AddSwaggerConfiguration(logger);
builder.Services.AddHealthCheckConfiguration(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Api Extensions ", logCounter++, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── 3. Appplication Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Application Extensions ", logCounter, logTotal);
builder.Services.AddUseCaseServices(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Aplication Extensions ", logCounter++, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── 4. Domain Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Domain Extensions ", logCounter, logTotal);
builder.Services.AddDomainServices(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Domain Extensions ", logCounter++, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── 5. Infrastructure Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Infrastructure Extensions ", logCounter, logTotal);
builder.Services.AddDbContext(builder.Configuration, logger);
builder.Services.AddCustomLogging(logger);
builder.Services.AddRepositories(logger);
builder.Services.AddAuditLog(builder.Configuration, logger);
builder.Services.AddMessaging(builder.Configuration, logger);
builder.Services.AddAuthenticationServices(builder.Configuration,logger);
builder.Services.AddCacheService(builder.Configuration, logger);
builder.Services.AddMetricsServices(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Infrastructure Extensions ", logCounter++, logTotal);


var app = builder.Build();


// ──────────────────────────────────────────────────────────────────────────────
// ── 6. Middlewares
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Middlewares ", logCounter, logTotal);
app.UseCorrelationMiddleware();
app.UseExceptionMiddleware();
app.UseTokenBlacklistMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwaggerMiddleware(logger);
app.UseMetricsMiddleware();
app.MapControllers();
app.MapCustomHealthChecks();
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Middlewares ", logCounter++, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── 7. Observability
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Metrics ", logCounter, logTotal);
app.UseMetricServer();
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Metrics ", logCounter++, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Migrations
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Migrations ", logCounter, logTotal);
app.ApplyMigrations(builder.Configuration, logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Migrations ", logCounter++, logTotal);




app.Run();




#region Helper Methods
void ConfigureAppSettings(ConfigurationManager config)
{
    string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                       ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                       ?? "Production"; // Define um default se não encontrar

    config.SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
          .AddEnvironmentVariables();
}
#endregion
