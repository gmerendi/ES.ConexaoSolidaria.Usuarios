using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Auth
{
    public class ResetarSenhaCommandHandler : IUseCaseHandler<ResetarSenhaCommand, Result<string>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IBaseLogger<ResetarSenhaCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly ICryptoService _cryptoService;
        private readonly ICacheService _cacheService;
        private readonly IUserContext _userContext;
        private readonly IMessageService _messageService;


        public ResetarSenhaCommandHandler(IUsuarioRepository usuarioRepository, IBaseLogger<ResetarSenhaCommandHandler> logger, 
            ITokenService tokenService, ICryptoService cryptoService, ICacheService cacheService, IUserContext userContext, 
            IMessageService messageService)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
            _tokenService = tokenService;
            _cryptoService = cryptoService;
            _cacheService = cacheService;
            _userContext = userContext;
            _messageService = messageService;
        }

        public async Task<Result<string>> HandleAsync(ResetarSenhaCommand command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.PasswordNovo))
            {
                throw new DomainException("400_PASSWORD_REQUIRED");
            }
                

            try
            {
                var solicitante = _userContext.GetUser() ?? null;
                var tempoExpiracao = _tokenService.GetTokenTimeToExpire(command.Token);

                _logger.LogInformation("Tentativa de resetar senha iniciada para o email: " + solicitante.Email, BaseLogType.LOG, command);

                // 1 - Somente o próprio usuário pode resetar a senha, mesmo que seja um GESTOR_ONG.
                var usuario = await _usuarioRepository.ObterPorEmailAsync(solicitante.Email);

                if (usuario == null)
                {
                    throw new DomainException("400_USER_NOT_FOUND");
                }

                if (usuario.Status == EntityStatus.SUSPENDED)
                {
                    throw new DomainException("403_USER_SUSPENDED");
                }

                // 2 - Por seguranca, verifica o password atual. O usuário já está autenticado,
                // mas isso garante que é realmente o dono do email e não alguém com acesso ao token.
                if (!_cryptoService.VerifyPassword(command.PasswordAtual, usuario.SenhaHash))
                {
                    throw new DomainException("403_PASSWORD_INCORRECT");
                }

                // 3 - Atualiza a senha do usuário
                var password = Password.CreatePasswordHash(command.PasswordNovo).Value;
                usuario.AlterarSenha(password, solicitante.Email);
                await _usuarioRepository.AlterarAsync(usuario, ct);

                // 4 - Insere Token do usuario na blacklist do cache, para invalidar o token antigo até expirar.
                // Assim, mesmo que o token antigo seja usado, ele não será mais válido. 
                await _cacheService.SetBlacklistAsync(command.Token, tempoExpiracao, ct);

                // 5 - Cria token novo
                (string newToken, DateTime expiracao) = _tokenService.GetToken(usuario);


                return Result<string>.Success(newToken);
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao resetar senha: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Erro ao resetar senha.");
            }
        }   
    }
}
