using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System.Text.Json;
using Usuarios.Application.Interfaces;

namespace Usuarios.Infrastructure.Services.AuditLog
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IDynamoDBContext _context;
        private readonly IBaseLogger<AuditLogService> _logger;
        public AuditLogService(IAmazonDynamoDB dynamoDbClient, IBaseLogger<AuditLogService> logger)
        {
            _context = new DynamoDBContext(dynamoDbClient);
            _logger = logger;
        }
        public async Task SaveLogAsync(string entityType, string entityId, string operation, string user, object data, string service)
        {
            try
            {
                _logger.LogInformation($"Preparando audit log para entidade {entityType} com Guid {entityId} - Operação: {operation} feito por: {user}", data);
                var entry = new AuditLog
                {
                    PK = $"ENTITY#{entityType.ToUpper()}#{entityId}",
                    SK = $"TS#{DateTime.UtcNow:O}",
                    ResourceId = entityId,
                    ServiceName = service,
                    Operation = operation,
                    ChangedBy = user,
                    Payload = JsonSerializer.Serialize(data),
                    // Define expiração para 1 ano (exemplo)
                    ExpirationTime = DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds()
                };
                _logger.LogInformation($"Salvando audit log para entidade {entityType} com Guid {entityId} - Operação: {operation} feito por: {user}", entry);

                await _context.SaveAsync(entry);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro preparando audit log para entidade {entityType} com Guid {entityId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task SaveRawLogAsync(AuditLog log)
        {
            try
            {
                _logger.LogInformation($"Salvando raw audit log para o ResourceId: {log.ResourceId} - Operação: {log.Operation} feito por: {log.ChangedBy}", log);
                await _context.SaveAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro preparando raw audit log para o ResourceId: {log.ResourceId}: {ex.Message}", ex);
                throw;
            }
        }
    }
}
