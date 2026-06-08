using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entities.Usuarios.DTO;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Auth
{
    public class LogarUsuarioCommandHandler : IUseCaseHandler<LogarUsuarioCommand, Result<LogarUsuarioResponse>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IBaseLogger<LogarUsuarioCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly ICryptoService _cryptoService;
        private readonly ICacheService _cacheService;

        public LogarUsuarioCommandHandler(IUsuarioRepository usuarioRepository,IBaseLogger<LogarUsuarioCommandHandler> logger,
            ITokenService tokenService, ICryptoService cryptoService, ICacheService cacheService)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
            _tokenService = tokenService;
            _cryptoService = cryptoService;
            _cacheService = cacheService;
        }

        public async Task<Result<LogarUsuarioResponse>> HandleAsync(LogarUsuarioCommand command, CancellationToken ct)
        {
            // 1 - Verificar se o command não é nulo 
            if (command == null)
            {
                throw new DomainException("400_COMMAND_INVALID");
            }

            try
            {
                _logger.LogInformation("Tentativa de login iniciada para o e-mail: " + command.Email, BaseLogType.LOG, command.Email);

                // 2 - Verificar se usuario já existe
                var usuario = await _usuarioRepository.ObterPorEmailAsync(command.Email, ct);

                if (usuario == null)
                { 
                    throw new DomainException("400_USER_NOT_FOUND");
                }

                if (usuario.Status == EntityStatus.SUSPENDED)
                {
                    throw new DomainException("403_USER_SUSPENDED");
                }
                if (usuario.Status == EntityStatus.REMOVED)
                {
                    throw new DomainException("403_USER_REMOVED");
                }

                // 3. Verifica se a senha bate usando o serviço BCrypt
                bool senhaValida = _cryptoService.VerifyPassword(command.Password, usuario.SenhaHash);

                if (!senhaValida)
                {
                    throw new DomainException("401_INVALID_CREDENTIALS");
                }

                // 4. Se passou, gera o Token JWT
                var (token, expiracao) = _tokenService.GetToken(usuario);

                // 5. Insere usuario no cache 
                var cacheKey = $"usuario:{usuario.Email.Endereco}";
                var usuarioCache = UsuarioDTO.FromEntity(usuario);
                await _cacheService.SetAsync(cacheKey, usuarioCache, TimeSpan.FromMinutes(30));

                // 6. Devolve o resultado elaborado para a controller
                var resultado = new LogarUsuarioResponse(token, expiracao, usuario.Guid, usuario.Email.Endereco, usuario.Status.ToString());
                return Result<LogarUsuarioResponse>.Success(resultado);
            }
            catch (DomainException)
            {
                throw; 
            }
            catch (Exception ex) {  
                _logger.LogError("Erro ao fazer login: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Ocorreu um erro ao fazer o login. " + ex.Message);
            }
        }   
    }
}
