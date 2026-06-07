using CS.Domain.Events;
using Usuarios.Application.Shared;
using Usuarios.Domain.Entity.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Usuarios
{
    public class RemoverUsuarioCommandHandler : IUseCaseHandler<RemoverUsuarioCommand, Result<bool>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUserContext _userContext;
        private readonly IBaseLogger<RemoverUsuarioCommandHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public RemoverUsuarioCommandHandler(IUsuarioRepository usuarioRepository, IUserContext userContext,
            IBaseLogger<RemoverUsuarioCommandHandler> logger, ICacheService cacheService, IMessageService messageService)
        {
            _usuarioRepository = usuarioRepository;
            _userContext = userContext;
            _logger = logger;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<bool>> HandleAsync(RemoverUsuarioCommand command, CancellationToken ct)
        {
            // 1 - Verificar se o command não é nulo 
            if (command == null)
            {
                throw new DomainException("400_COMMAND_INVALID");
            }

            try
            {
                _logger.LogInformation("Tentativa de remocao de usuario iniciada para o email: " + command.Email, BaseLogType.LOG, command);

                //2 - Buscar solicitante. Um usuario, seja ele GESTOR_ONG ou DOADOR, somente pode remover o próprio perfil
                // O GESTOR_ONG, caso deseje desabilitar o perfil, deve modificar o status para SUSPENDED
                var solicitante = _userContext.GetUser() ?? null;

                if (solicitante == null) 
                {
                    throw new DomainException("400_REQUESTER_REQUIRED");
                }

                // 3 - Remove usuario do banco
                var usuario = await _usuarioRepository.ObterPorEmailAsync(command.Email);

                if (usuario == null)
                {
                    throw new DomainException("400_USER_NOT_FOUND");
                }

                // 4 - Remove usuario do cache
                var cacheKey = $"usuario:{command.Email}";
                await _cacheService.RemoveAsync(cacheKey);

                // 5 - Envia evento de remoção de usuário
                await _messageService.SendUserRemovedEventMessage(usuario.Guid, usuario.NomeCompleto, usuario.Email.Endereco, usuario.Cpf.Numero, ct);

                return Result<bool>.Success(true);
            }
            catch (DomainException)
            {
                throw; 
            }
            catch (Exception ex) {  
                _logger.LogError("Erro ao remover usuario: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Ocorreu um erro ao remover  o usuário. " + ex.Message);
            }
        }   
    }
}
