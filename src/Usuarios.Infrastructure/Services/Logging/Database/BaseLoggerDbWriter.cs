using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Usuarios.Infrastructure.Services.Logging;

/// <summary>
/// Implementação de <see cref="IBaseLoggerDbWriter"/> que persiste logs no DynamoDB.
/// Para trocar de banco (ex: PostgreSQL, Elasticsearch), basta modificar
/// a implementação dessa classe — o BaseLogger não muda.
/// </summary>
public class BaseLoggerDbWriter : IBaseLoggerDbWriter
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<BaseLoggerDbWriter> _logger;
    private readonly string _tableName;

    public BaseLoggerDbWriter(
        IAmazonDynamoDB dynamoDb,
        IConfiguration configuration,
        ILogger<BaseLoggerDbWriter> logger)
    {
        _dynamoDb = dynamoDb;
        _logger = logger;
        _tableName = configuration["DynamoDB:LogTable"] ?? "cs-app-log";
    }

    public Task WriteAsync(LogEntry entry)
    {
        // Fire-and-forget — não bloqueia o fluxo principal
        Task.Run(async () =>
        {
            try
            {
                var tabela = Table.LoadTable(_dynamoDb, _tableName);

                var doc = new Document
                {
                    ["CorrelationId"] = entry.CorrelationId,
                    ["Timestamp"] = entry.Timestamp,
                    ["Type"] = entry.Type,
                    ["LogLevel"] = entry.LogLevel,
                    ["Caller"] = entry.Caller,
                    ["Template"] = entry.Template,
                    ["Message"] = entry.Message,
                    ["Properties"] = entry.PropertiesJson,
                };

                await tabela.PutItemAsync(doc);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Erro ao persistir log no DynamoDB.");
            }
        });

        return Task.CompletedTask;
    }
}