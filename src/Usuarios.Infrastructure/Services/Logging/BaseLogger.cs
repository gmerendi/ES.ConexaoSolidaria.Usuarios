using Usuarios.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Usuarios.Infrastructure.Services.Logging
{
    public class BaseLogger<T> : IBaseLogger<T>
    {
        protected readonly ILogger<T> _logger;
        protected readonly ICorrelationIdGenerator _correlationIdGenerator;
        protected readonly IConfiguration _configuration;

        public BaseLogger(ILogger<T> logger, ICorrelationIdGenerator correlationIdGenerator, IConfiguration configuration)
        {
            _logger = logger;
            _correlationIdGenerator = correlationIdGenerator;
            _configuration = configuration;
        }

        private string ResolveCorrelationId(string? passedCorrelationId)
        {
            if (!string.IsNullOrWhiteSpace(passedCorrelationId))
            {
                _correlationIdGenerator.Set(passedCorrelationId);
                return passedCorrelationId;
            }

            var currentId = _correlationIdGenerator.Get();

            if (string.IsNullOrWhiteSpace(currentId))
            {
                currentId = Guid.NewGuid().ToString();
                _correlationIdGenerator.Set(currentId);
            }

            return currentId;
        }

        public virtual void LogInformation(string message, object? data, string? correlationId)
        {
            var custom = _configuration["CustomLogging:LogInfo"] == "True";
            if (_configuration["CustomLogging:LogInfo"] == "True")
            {
                var activeId = ResolveCorrelationId(correlationId);
                _logger.LogInformation("[CorrelationId: {CorrelationId}] {Message} | Data: {@Data}", activeId, message, data);
            }
        }

        public virtual void LogError(string message, object? data, string? correlationId)
        {
            if (_configuration["CustomLogging:LogError"] == "True")
            {
                var activeId = ResolveCorrelationId(correlationId);
                _logger.LogError("[CorrelationId: {CorrelationId}] {Message} | Data: {@Data}", activeId, message, data);
            }
        }

        public virtual void LogWarning(string message, object? data, string? correlationId)
        {
            if (_configuration["CustomLogging:LogWarning"] == "True")
            {
                var activeId = ResolveCorrelationId(correlationId);
                _logger.LogWarning("[CorrelationId: {CorrelationId}] {Message} | Data: {@Data}", activeId, message, data);
            }
        }
    }
}