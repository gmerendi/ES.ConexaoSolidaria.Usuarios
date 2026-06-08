using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entities.Usuarios.DTO;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Usuarios
{
    public class ModificarUsuarioCommandHandler : IUseCaseHandler<ModificarUsuarioCommand, Result<ModificarUsuarioResponse>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUserContext _userContext;
        private readonly IBaseLogger<ModificarUsuarioCommandHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly IUsuarioDomainService _usuarioDomainService;

        public ModificarUsuarioCommandHandler(IUsuarioRepository usuarioRepository, IUserContext userContext,
            IBaseLogger<ModificarUsuarioCommandHandler> logger, ICacheService cacheService, IMessageService messageService, 
            IUsuarioDomainService usuarioDomainService)
        {
            _usuarioRepository = usuarioRepository;
            _userContext = userContext;
            _logger = logger;
            _cacheService = cacheService;
            _messageService = messageService;
            _usuarioDomainService = usuarioDomainService;
        }

        public async Task<Result<ModificarUsuarioResponse>> HandleAsync(ModificarUsuarioCommand command, CancellationToken ct)
        {
            // 1 - Verificar se o command não é nulo 
            if (command == null)
            {
                throw new DomainException("400_COMMAND_INVALID");
            }

            try
            {
                var solicitante = _userContext.GetUser() ?? null;
                _logger.LogInformation("Tentativa de modificacao de usuario iniciada para o email: " + solicitante.Email, BaseLogType.LOG, command);

                //2 - Buscar solicitante. O usuario modifica somente a si mesmo          
                var usuarioSolicitante = await _usuarioRepository.ObterPorEmailAsync(solicitante.Email);
                var usuario = await _usuarioRepository.ObterPorEmailAsync(solicitante.Email);

                if (usuario == null)
                {
                    throw new DomainException("400_USER_NOT_FOUND");
                }

                if (solicitante == null) 
                {
                    throw new DomainException("400_REQUESTER_REQUIRED");
                }

                _usuarioDomainService.PodeAlterarUsuario(usuarioSolicitante, usuario);

                // 3 - Modifica o cpf e nome do usuario
                usuario.AlterarUsuario(command.NomeCompleto, Cpf.Create(command.Cpf), usuarioSolicitante.Email.Endereco);
                await _usuarioRepository.AlterarAsync(usuario);

                // 4 - Remove usuario do cache
                var cacheKey = $"usuario:{usuario.Email.Endereco}";
                await _cacheService.RemoveAsync(cacheKey);

                UsuarioDTO usuarioResponse = UsuarioDTO.FromEntity(usuario);
                var response = new ModificarUsuarioResponse
                    (
                        usuarioResponse.Guid,
                        usuarioResponse.NomeCompleto,
                        usuarioResponse.Cpf,
                        usuarioResponse.Email,
                        usuarioResponse.Perfil,
                        usuarioResponse.Status
                    );
               

                return Result<ModificarUsuarioResponse>.Success(response);
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
