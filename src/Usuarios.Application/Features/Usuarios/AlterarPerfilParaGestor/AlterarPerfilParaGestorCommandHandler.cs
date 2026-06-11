using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Usuarios
{
    public sealed class AlterarPerfilParaGestorCommandHandler : IUseCaseHandler<AlterarPerfilParaGestorCommand, Result<bool>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUserContext _userContext;
        private readonly IBaseLogger<AlterarPerfilParaGestorCommandHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly IUsuarioDomainService _usuarioDomainService;

        public AlterarPerfilParaGestorCommandHandler(IUsuarioRepository usuarioRepository, IUserContext userContext,
            IBaseLogger<AlterarPerfilParaGestorCommandHandler> logger, ICacheService cacheService, IMessageService messageService, 
            IUsuarioDomainService usuarioDomainService)
        {
            _usuarioRepository = usuarioRepository;
            _userContext = userContext;
            _logger = logger;
            _cacheService = cacheService;
            _messageService = messageService;
            _usuarioDomainService = usuarioDomainService;
        }

        public async Task<Result<bool>> HandleAsync(AlterarPerfilParaGestorCommand command, CancellationToken ct)
        {
            // 1 - Verificar se o command não é nulo 
            if (command == null)
            {
                throw new DomainException("400_COMMAND_INVALID");
            }

            try
            {
                _logger.LogInformation("Tentativa de alteracao de perfil para gestor iniciada para o email: " + command.Email, BaseLogType.LOG, command);

                //2 - Buscar solicitante. Somente GESTOR_ONG pode suspender um usuario.
                // Porem um GESTOR_ONG não pode suspender o seu proprio perfil.               
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

                // 3 - Modifica o perfil do usuario para GESTOR_ONG
                usuario.AlterarPerfilParaGestor(solicitante.Email);    
                await _usuarioRepository.AlterarAsync(usuario);

                // 5 - Remove usuario do cache
                var cacheKey = $"usuario:{command.Email}";
                await _cacheService.RemoveAsync(cacheKey);

                return Result<bool>.Success(true);
            }
            catch (DomainException)
            {
                throw; 
            }
            catch (Exception ex) {  
                _logger.LogError("Erro ao alterar perfil para gestor: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Ocorreu um erro ao alterar o perfil para gestor. " + ex.Message);
            }
        }   
    }
}
