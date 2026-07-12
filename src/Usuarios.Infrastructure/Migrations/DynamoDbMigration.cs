using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Usuarios.Infrastructure.Migrations;

public static class DynamoDbConfiguration
{
    public static async Task DynamoDbMigration(IServiceProvider services)
    {
        var client = services.GetRequiredService<IAmazonDynamoDB>();
        var tables = await client.ListTablesAsync();

        // ── 1. Audit Log ────────────────────────────────────────────────────
        // Schema:
        //   PK (S)          → "ENTITY#{EntityType}#{Guid}"
        //   SK (S)          → "TS#{ISO8601}"
        //   ServiceName (S) → microsserviço de origem
        //   Operation   (S) → ADDED | MODIFIED | DELETED
        //   ChangedBy   (S) → e-mail do usuário
        //   ResourceId  (S) → guid da entidade alterada
        //   IpAddress   (S) → IP de origem
        //   Payload     (S) → snapshot JSON da entidade
        //   TTL         (N) → Unix timestamp para expiração automática
        await CriarTabelaSeNaoExistirAsync(
            client, tables.TableNames,
            "cs-audit-log",
            hashKey: "PK",
            rangeKey: "SK");

        // ── 2. Application Logs ─────────────────────────────────────────────
        // Schema:
        //   CorrelationId (S) → trace ID que correlaciona todos os logs de uma requisição
        //   Timestamp     (S) → ISO 8601 UTC — chave de ordenação
        //   LogLevel      (S) → Information | Warning | Error
        //   Type          (N) → enum BaseLogType (0=Log, 1=Event, ...)
        //   Caller        (S) → FQDN da classe que gerou o log
        //   Template      (S) → message template original: "Login de {Email} via {Ip}"
        //   Message       (S) → mensagem renderizada com os valores
        //   Properties    (S) → JSON estruturado com as propriedades nomeadas do template
        //   TTL           (N) → Unix timestamp para expiração automática
        //
        // GSIs disponíveis:
        //   GSI_EventSourcing  → Type (HASH)     + Timestamp (RANGE) — filtra por tipo de log
        //   GSI_LogLevel       → LogLevel (HASH) + Timestamp (RANGE) — filtra por nível
        //   GSI_Caller         → Caller (HASH)   + Timestamp (RANGE) — filtra por classe
        var gsis = new List<GlobalSecondaryIndex>
        {
            CriarGsi("GSI_EventSourcing", hashKey: "Type",     hashType: ScalarAttributeType.N, rangeKey: "Timestamp"),
            CriarGsi("GSI_LogLevel",      hashKey: "LogLevel", hashType: ScalarAttributeType.S, rangeKey: "Timestamp"),
            CriarGsi("GSI_Caller",        hashKey: "Caller",   hashType: ScalarAttributeType.S, rangeKey: "Timestamp"),
        };

        await CriarTabelaSeNaoExistirAsync(
            client, tables.TableNames,
            "cs-app-log",
            hashKey: "CorrelationId",
            rangeKey: "Timestamp",
            gsis: gsis);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════════

    private static GlobalSecondaryIndex CriarGsi(
        string indexName,
        string hashKey,
        ScalarAttributeType hashType,
        string rangeKey) =>
        new()
        {
            IndexName = indexName,
            KeySchema = new List<KeySchemaElement>
            {
                new(hashKey,  KeyType.HASH),
                new(rangeKey, KeyType.RANGE),
            },
            Projection = new Projection { ProjectionType = ProjectionType.ALL },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
        };

    private static async Task CriarTabelaSeNaoExistirAsync(
        IAmazonDynamoDB client,
        List<string> tabelasExistentes,
        string tableName,
        string hashKey,
        string rangeKey,
        List<GlobalSecondaryIndex>? gsis = null)
    {
        if (tabelasExistentes.Contains(tableName)) return;

        // Atributos base (PK e SK da tabela)
        var atributos = new List<AttributeDefinition>
        {
            new(hashKey,  ScalarAttributeType.S),
            new(rangeKey, ScalarAttributeType.S),
        };

        // Adiciona atributos dos GSIs que ainda não estão declarados
        if (gsis is { Count: > 0 })
        {
            var atributosDeclarados = new HashSet<string> { hashKey, rangeKey };

            foreach (var gsi in gsis)
                foreach (var key in gsi.KeySchema)
                {
                    if (atributosDeclarados.Contains(key.AttributeName)) continue;

                    // Determina o tipo do atributo a partir da configuração do GSI
                    // (GSIs guardam o tipo de cada chave via Projection — aqui inferimos
                    //  pelo nome ou buscamos no próprio GSI se disponível)
                    var tipo = InferirTipoAtributo(key.AttributeName, gsis);
                    atributos.Add(new AttributeDefinition(key.AttributeName, tipo));
                    atributosDeclarados.Add(key.AttributeName);
                }
        }

        var request = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = atributos,
            KeySchema = new List<KeySchemaElement>
            {
                new(hashKey,  KeyType.HASH),
                new(rangeKey, KeyType.RANGE),
            },
            GlobalSecondaryIndexes = gsis,
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
        };

        await client.CreateTableAsync(request);

        // TTL automático — evita acúmulo infinito de logs antigos
        await client.UpdateTimeToLiveAsync(new UpdateTimeToLiveRequest
        {
            TableName = tableName,
            TimeToLiveSpecification = new TimeToLiveSpecification
            {
                Enabled = true,
                AttributeName = "TTL",
            },
        });
    }

    /// <summary>
    /// Infere o ScalarAttributeType de um campo a partir dos GSIs configurados.
    /// Campos com nomes conhecidos têm tipo fixo; demais são tratados como String.
    /// </summary>
    private static ScalarAttributeType InferirTipoAtributo(
        string attributeName,
        List<GlobalSecondaryIndex> gsis)
    {
        // Campos numéricos conhecidos
        if (attributeName is "Type" or "TTL")
            return ScalarAttributeType.N;

        // Campos string conhecidos (LogLevel, Caller, Timestamp, etc.)
        return ScalarAttributeType.S;
    }
}