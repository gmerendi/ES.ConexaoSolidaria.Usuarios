using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Usuarios.Infrastructure.Migrations
{
    public static class DynamoDbConfiguration
    {
        public static async Task DynamoDbMigration(IServiceProvider services)
        {
            var client = services.GetRequiredService<IAmazonDynamoDB>();
            var tables = await client.ListTablesAsync();

            // 1. Tabela de Audit Log: Configurada para armazenar logs de auditoria
            await CriarTabelaSeNaoExistirAsync(client, tables.TableNames, "cs-audit-log", "PK", "SK");

            // 2. Tabela de logs de aplicação e Event Sourcing
            var gsiEventSourcing = new GlobalSecondaryIndex
            {
                IndexName = "GSI_EventSourcing",
                KeySchema = new List<KeySchemaElement>
                {
                    new("Type", KeyType.HASH),    // Permite filtrar por Log (0) ou Evento (1)
                    new("Timestamp", KeyType.RANGE) 
                },
                Projection = new Projection { ProjectionType = ProjectionType.ALL },
                ProvisionedThroughput = new ProvisionedThroughput(5, 5)
            };

            await CriarTabelaSeNaoExistirAsync(
                client,
                tables.TableNames,
                "cs-app-log",
                "CorrelationId",
                "Timestamp",
                gsiEventSourcing
            );
        }

        private static async Task CriarTabelaSeNaoExistirAsync(
            IAmazonDynamoDB client,
            List<string> tabelasExistentes,
            string tableName,
            string hashKey,
            string rangeKey,
            GlobalSecondaryIndex? gsi = null) // Parâmetro opcional para passar o GSI
        {
            if (tabelasExistentes.Contains(tableName)) return;

            // Definição de atributos base da tabela
            var attributeDefinitions = new List<AttributeDefinition>
            {
                new(hashKey, ScalarAttributeType.S),
                new(rangeKey, ScalarAttributeType.S)
            };

            var keySchema = new List<KeySchemaElement>
            {
                new(hashKey, KeyType.HASH),
                new(rangeKey, KeyType.RANGE)
            };

            var createTableRequest = new CreateTableRequest
            {
                TableName = tableName,
                AttributeDefinitions = attributeDefinitions,
                KeySchema = keySchema,
                ProvisionedThroughput = new ProvisionedThroughput(5, 5)
            };

            
            if (gsi != null)
            {
                createTableRequest.GlobalSecondaryIndexes = new List<GlobalSecondaryIndex> { gsi };

                
                foreach (var element in gsi.KeySchema)
                {
                    
                    if (element.AttributeName == "Type")
                    {
                        attributeDefinitions.Add(new AttributeDefinition("Type", ScalarAttributeType.N));
                    }
                    else if (element.AttributeName != hashKey && element.AttributeName != rangeKey)
                    {
                        attributeDefinitions.Add(new AttributeDefinition(element.AttributeName, ScalarAttributeType.S));
                    }
                }
            }

            // Criação estruturada da tabela
            await client.CreateTableAsync(createTableRequest);

            // Ativação do TTL (Time to Live) padrão para evitar acúmulo desnecessário de logs antigos
            await client.UpdateTimeToLiveAsync(new UpdateTimeToLiveRequest
            {
                TableName = tableName,
                TimeToLiveSpecification = new TimeToLiveSpecification { Enabled = true, AttributeName = "TTL" }
            });
        }
    }
}