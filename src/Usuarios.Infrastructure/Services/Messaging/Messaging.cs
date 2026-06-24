using Amazon.SQS;
using Amazon.SQS.Model;
using CS.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
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
        private readonly IAmazonSQS _sqsClient;


        public MessageService(IPublishEndpoint publish, IConfiguration configuration,
            IBaseLogger<MessageService> logger, ICorrelationIdGenerator correlationIdGenerator,
            IAmazonSQS sqsClient)
        {
            _publish = publish;
            _userCreatedQueueUrl = Environment.GetEnvironmentVariable("USER_CREATED_QUEUE")
                                   ?? configuration["USER_CREATED_QUEUE"]
                                   ?? "user-queue-failed";
            _applicationType = Environment.GetEnvironmentVariable("Application__Type")
                                   ?? configuration["Application__Type"]
                                   ?? "application_type_failed";
            _correlationIdGenerator = correlationIdGenerator;
            _logger = logger;
            _sqsClient = sqsClient;
        }



        public async Task SendUserCreatedEventMessage(Guid guidUser, string nome, string email, string cpf, CancellationToken ct)
        {

            if (_applicationType == "LOCAL")
            {
                await SendUserCreatedEventMessageRabbit(guidUser, nome, email, cpf, ct);
            } 
            else if (_applicationType == "LAB")
            {
                await SendUserCreatedEventMessageSQS(guidUser, nome, email, cpf, ct);
            }

        }







        // -----------------------------------------------------------------------------
        // Privados
        // -----------------------------------------------------------------------------
        // Rabbit
        private async Task SendUserCreatedEventMessageRabbit(Guid guidUser, string nome, string email, string cpf, CancellationToken ct)
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




        // SQS
        private async Task SendUserCreatedEventMessageSQS(Guid guidUser, string nome, string email, string cpf, CancellationToken ct)
        {
            var message = new
            {
                guidUser = guidUser.ToString(),
                nome = nome,
                email = email,
                cpf = Cpf.Anonymize(cpf),
                correlationId = _correlationIdGenerator.Get()
            };           

            try
            {
                var messageBody = JsonSerializer.Serialize(message);
                var response = await _sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = _userCreatedQueueUrl,
                    MessageBody = messageBody  
                });
                _logger.LogInformation("Evento UserCreatedEvent publicado para o SQS. Email: " + email, BaseLogType.EVENT, message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao publicar evento UserCreatedEvent para o SQS : " + email, BaseLogType.EVENT, ex);
                throw;
            }

        }
    }
}
