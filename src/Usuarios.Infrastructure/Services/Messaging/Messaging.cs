using CS.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Interfaces;

namespace Usuarios.Infrastructure.Services.Messaging
{
    public class MessageService : IMessageService
    {
        private readonly IPublishEndpoint _publish;
        private readonly string _userCreatedQueueUrl;
        private readonly string _userRemovedQueueUrl;
        private readonly string _applicationType;
        private readonly IBaseLogger<MessageService> _logger;
        private readonly ICorrelationIdGenerator _correlationIdGenerator;


        public MessageService(IPublishEndpoint publish, IConfiguration configuration
            , IBaseLogger<MessageService> logger, ICorrelationIdGenerator correlationIdGenerator)
        {
            _publish = publish;
            _userCreatedQueueUrl = Environment.GetEnvironmentVariable("USER_CREATED_QUEUE")
                                   ?? configuration["USER_CREATED_QUEUE"]
                                   ?? "user-queue-failed";
            _userRemovedQueueUrl = Environment.GetEnvironmentVariable("USER_REMOVED_QUEUE")
                                   ?? configuration["USER_REMOVED_QUEUE"]
                                   ?? "user-queue-failed";
            _applicationType = Environment.GetEnvironmentVariable("Application__Type")
                                   ?? configuration["Application__Type"]
                                   ?? "application_type_failed";
            _correlationIdGenerator = correlationIdGenerator;
            _logger = logger;
        }



        public async Task SendUserCreatedEventMessage(Guid guidUser, string nome, string email, string cpf, CancellationToken ct)
        {

            try
            {
                var eventMessage = new UserCreatedEvent(guidUser, nome, email, Cpf.Anonymize(cpf), _correlationIdGenerator.Get());
                await _publish.Publish(eventMessage, ct);
                _logger.LogInformation("Evento UserCreatedEvent publicado para o Broker. Email: " + email, BaseLogType.EVENT, eventMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao publicar evento UserCreatedEvent para o Broker : " + email, BaseLogType.EVENT, ex);
                throw;
            }

        }        
    }
}
