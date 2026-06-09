using Prometheus;
using Usuarios.Api.Configuration;
using Usuarios.Api.Extensions;
using Usuarios.Api.Middlewares;
using Usuarios.Application.Extensions;
using Usuarios.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
var logCounter = 0;
var logTotal = 10;

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
logger.LogInformation(" ***** ({0}/{1}) - Inicializando Users API ", logCounter++, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Api
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Endpoints ", logCounter++, logTotal);
builder.Services.AddControllers().ConfigurarMensagensDeValidacaoCustomizadas().ConfigurarErrosDeValidacaoCustomizados();
builder.Services.AddEndpointsApiExplorer();
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Endpoints ", logCounter, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Api Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Api Extensions ", logCounter++, logTotal);
builder.Services.AddSwaggerConfiguration(logger);
builder.Services.AddHealthCheckConfiguration(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Api Extensions ", logCounter, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Appplication Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Api Extensions ", logCounter++, logTotal);
builder.Services.AddUseCaseServices(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Api Extensions ", logCounter, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Domain Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Api Extensions ", logCounter++, logTotal);
builder.Services.AddDomainServices(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Api Extensions ", logCounter, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Infrastructure Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Infrastructure Extensions ", logCounter++, logTotal);
builder.Services.AddDbContext(builder.Configuration, logger);
builder.Services.AddCustomLogging(logger);
builder.Services.AddRepositories(logger);
builder.Services.AddAuditLog(builder.Configuration, logger);
builder.Services.AddMessaging(builder.Configuration, logger);
builder.Services.AddAuthenticationServices(builder.Configuration,logger);
builder.Services.AddCacheService(builder.Configuration, logger);
builder.Services.AddMetricsServices(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Infrastructure Extensions ", logCounter, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Health Check Service
// ──────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();

// ──────────────────────────────────────────────────────────────────────────────
// ── Observability
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Metrics ", logCounter++, logTotal);
app.UseMetricServer();
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Metrics ", logCounter, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Middlewares
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Middlewares ", logCounter++, logTotal);
app.UseSwaggerMiddleware(logger);
app.UseCorrelationMiddleware();
app.UseExceptionMiddleware();
app.UseTokenBlacklistMiddleware();
app.UseMetricsMiddleware();
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Middlewares ", logCounter, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Migrations
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Migrations ", logCounter++, logTotal);
app.ApplyMigrations(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Migrations ", logCounter, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Health Check MAppings
// ──────────────────────────────────────────────────────────────────────────────
app.MapCustomHealthChecks();



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
