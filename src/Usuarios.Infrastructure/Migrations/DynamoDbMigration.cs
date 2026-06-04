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
            var tableName = "cs-audit-log";

            var tables = await client.ListTablesAsync();
            if (!tables.TableNames.Contains(tableName))
            {
                await client.CreateTableAsync(new CreateTableRequest
                {
                    TableName = tableName,
                    AttributeDefinitions = new List<AttributeDefinition>
            {
                new("PK", ScalarAttributeType.S),
                new("SK", ScalarAttributeType.S)
            },
                    KeySchema = new List<KeySchemaElement>
            {
                new("PK", KeyType.HASH),
                new("SK", KeyType.RANGE)
            },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                });

                // Opcional: Ativar o TTL via código
                await client.UpdateTimeToLiveAsync(new UpdateTimeToLiveRequest
                {
                    TableName = tableName,
                    TimeToLiveSpecification = new TimeToLiveSpecification { Enabled = true, AttributeName = "TTL" }
                });
            }
        }
    }
}
