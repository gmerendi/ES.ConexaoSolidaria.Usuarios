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
        private readonly ICryptoService _cryptoService;
        private readonly ICacheService _cacheService;
        private readonly IUserContext _userContext;


        public DeslogarUsuarioCommandHandler(IUsuarioRepository usuarioRepository, IBaseLogger<DeslogarUsuarioCommandHandler> logger, 
            ITokenService tokenService, ICryptoService cryptoService, ICacheService cacheService, IUserContext userContext)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
            _tokenService = tokenService;
            _cryptoService = cryptoService;
            _cacheService = cacheService;
            _userContext = userContext;
        }

        public async Task<Result<bool>> HandleAsync(DeslogarUsuarioCommand command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.Token))
            {
                throw new DomainException("400_TOKEN_REQUIRED");
            }
                

            try
            {
                var solicitante = _userContext.GetUser() ?? null;
                _logger.LogInformation("Tentativa de deslogar usuario iniciada para o email: " + solicitante.Email, BaseLogType.LOG, command);

                var tempoExpiracao= _tokenService.GetTokenTimeToExpire(command.Token);

                // Se o token já expirou naturalmente, não precisa gastar memória do Redis
                if (tempoExpiracao.TotalSeconds > 0)
                {
                    // Salva no Redis usando o próprio token como chave e o tempo restante como TTL (Time-To-Live)
                    // Quando o tempo restante acabar, o Redis deleta o token sozinho.
                    await _cacheService.SetBlacklistAsync(command.Token, tempoExpiracao, ct);

                    // Remove usuario do cache para forçar nova consulta ao banco na próxima autenticação
                    await _cacheService.RemoveAsync($"usuario:{solicitante.Email}");

                    _logger.LogInformation("Adicionando token à blacklist do Redis.", BaseLogType.LOG, command);
                }

                return Result<bool>.Success(true);
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao efetuar logout: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Erro ao efetuar logout.");
            }
        }   
    }
}
