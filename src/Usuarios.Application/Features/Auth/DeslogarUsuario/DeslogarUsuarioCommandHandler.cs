using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Auth
{
    public class DeslogarUsuarioCommandHandler : IUseCaseHandler<DeslogarUsuarioCommand, Result<bool>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IBaseLogger<DeslogarUsuarioCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cacheService;
        private readonly IUserContext _userContext;
        private readonly IMetricsService _metrics;

        public DeslogarUsuarioCommandHandler(IUsuarioRepository usuarioRepository,
            IBaseLogger<DeslogarUsuarioCommandHandler> logger,
            ITokenService tokenService, ICacheService cacheService,
            IUserContext userContext, IMetricsService metrics)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
            _tokenService = tokenService;
            _cacheService = cacheService;
            _userContext = userContext;
            _metrics = metrics;
        }

        public async Task<Result<bool>> HandleAsync(DeslogarUsuarioCommand command, CancellationToken ct)
        {
            if (command == null)
                throw new DomainException("400_COMMAND_INVALID");

            try
            {
                var solicitante = _userContext.GetUser();
                _logger.LogInformation("Tentativa de logout iniciada para o email: " + solicitante?.Email, BaseLogType.LOG, command);

                var tempoExpiracao = _tokenService.GetTokenTimeToExpire(command.Token);
                await _cacheService.SetBlacklistAsync(command.Token, tempoExpiracao, ct);

                if (solicitante != null)
                {
                    var cacheKey = $"usuario:{solicitante.Email}";
                    await _cacheService.RemoveAsync(cacheKey);
                }

                // ── Métrica de negócio ─────────────────────────────────────────
                _metrics.IncrementarLogout();

                return Result<bool>.Success(true);
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao fazer logout: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Ocorreu um erro ao fazer o logout. " + ex.Message);
            }
        }
    }
}