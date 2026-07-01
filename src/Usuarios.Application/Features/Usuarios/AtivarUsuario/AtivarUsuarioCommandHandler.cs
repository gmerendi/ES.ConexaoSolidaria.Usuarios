using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Usuarios
{
    public sealed class AtivarUsuarioCommandHandler : IUseCaseHandler<AtivarUsuarioCommand, Result<bool>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUserContext _userContext;
        private readonly IBaseLogger<AtivarUsuarioCommandHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUsuarioDomainService _usuarioDomainService;
        private readonly ICacheService _cacheService;

        public AtivarUsuarioCommandHandler(IUsuarioRepository usuarioRepository, IUserContext userContext,
            IBaseLogger<AtivarUsuarioCommandHandler> logger, IMessageService messageService, IUsuarioDomainService usuarioDomainService,
            ICacheService cacheService)
        {
            _usuarioRepository = usuarioRepository;
            _userContext = userContext;
            _logger = logger;
            _messageService = messageService;
            _usuarioDomainService = usuarioDomainService;
            _cacheService = cacheService;

        }

        public async Task<Result<bool>> HandleAsync(AtivarUsuarioCommand command, CancellationToken ct)
        {
            // 1 - Verificar se o command não é nulo 
            if (command == null)
            {
                throw new DomainException("400_COMMAND_INVALID");
            }

            try
            {
                _logger.LogInformation("Tentativa de ativação de usuario iniciada para o email: " + command.Email, BaseLogType.LOG, command);

                //2 - Buscar solicitante. Somente GESTOR_ONG pode ativar um usuario.
                // Porem um GESTOR_ONG não pode ativar o seu proprio perfil.               
                var solicitante = _userContext.GetUser() ?? null;
                var usuarioSolicitante = await _usuarioRepository.ObterPorEmailAsync(solicitante.Email);
                var usuario = await _usuarioRepository.ObterPorEmailAsync(command.Email);

                if (usuario == null)
                {
                    throw new DomainException("400_USER_NOT_FOUND");
                }

                if (solicitante == null) 
                {
                    throw new DomainException("400_REQUESTER_REQUIRED");
                }

                _usuarioDomainService.PodeAlterarPerfilEStatus(usuarioSolicitante, usuario);         

                // 3 - Modifica o status do usuario para ACTIVE
                usuario.Ativar(solicitante.Email);
                await _usuarioRepository.AlterarAsync(usuario);

                // 4 - Remove usuario do cache
                var cacheKey = $"usuario:{command.Email}";
                await _cacheService.RemoveAsync(cacheKey);

                return Result<bool>.Success(true);
            }
            catch (DomainException)
            {
                throw; 
            }
            catch (Exception ex) {  
                _logger.LogError("Erro ao ativar usuario: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Ocorreu um erro ao ativar  o usuário. " + ex.Message);
            }
        }   
    }
}
